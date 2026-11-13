import { defineStore } from "pinia";
import { ElNotification } from "element-plus";
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { systemMessageApi } from "@/api/modules/system/systemMessage";
import { useUserStore } from "@/stores/modules/user";
import { API_BASE_URL } from "@/utils";

interface UnreadCount {
  notices: number;
  inbox: number;
  total: number;
}

/** 与后端 SystemMessageNotifier 的事件名保持一致 */
const EVENT_RECEIVED = "message:received";
const EVENT_UNREAD = "message:unread";
const EVENT_REMOVED = "message:removed";

/** 降级轮询间隔：仅在与服务端断开时启用（正常走 WebSocket 推送，不轮询） */
const FALLBACK_POLL_INTERVAL = 120000;

export const useNoticeStore = defineStore("qps-notice", {
  state: () => ({
    unread: { notices: 0, inbox: 0, total: 0 } as UnreadCount,
    // 推送连接
    connection: null as HubConnection | null,
    connected: false,
    // 每收到一条新消息 +1，铃铛列表据此刷新（避免在 store 里存列表数据）
    revision: 0,
    // 降级轮询定时器
    pollingTimer: null as ReturnType<typeof setInterval> | null
  }),
  getters: {},
  actions: {
    // 拉取未读数（静默失败，不打扰用户）
    async refreshUnread() {
      try {
        const res = await systemMessageApi.getUnreadCount();
        const data = res?.data ?? res;
        if (data && typeof data.total === "number") {
          this.unread = {
            notices: data.notices ?? 0,
            inbox: data.inbox ?? 0,
            total: data.total ?? 0
          };
        }
      } catch {
        // 未登录 / 网络异常时保持现值
      }
    },

    // 建立实时推送连接：登录后调用，重复调用自动去重
    async connect() {
      const userStore = useUserStore();
      if (!userStore.token) return;
      if (this.connection) return;

      const connection = new HubConnectionBuilder()
        .withUrl(`${API_BASE_URL}/hubs/system-messages`, {
          // WebSocket 无法带请求头，后端从 access_token 查询参数取令牌
          accessTokenFactory: () => userStore.token,
          withCredentials: false
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(LogLevel.Warning)
        .build();

      connection.on(EVENT_RECEIVED, (message: any) => {
        this.revision += 1;
        // 推送只是提醒，角标以服务端为准（避免本地累加漂移）
        this.refreshUnread();
        ElNotification({
          title: message?.title || "新消息",
          message: message?.content || "",
          type: message?.messageType === 1 ? "info" : "success",
          duration: 6000,
          position: "bottom-right"
        });
      });

      // 多端同步：另一台设备读了消息，本端角标跟着变
      connection.on(EVENT_UNREAD, (count: any) => {
        if (count && typeof count.total === "number") {
          this.unread = {
            notices: count.notices ?? 0,
            inbox: count.inbox ?? 0,
            total: count.total ?? 0
          };
        }
      });

      // 公告下架 / 删除：刷新列表与角标
      connection.on(EVENT_REMOVED, () => {
        this.revision += 1;
        this.refreshUnread();
      });

      connection.onreconnecting(() => {
        this.connected = false;
        this.startPolling();
      });

      connection.onreconnected(() => {
        this.connected = true;
        this.stopPolling();
        // 断线期间可能漏推送，重连后补拉一次
        this.refreshUnread();
        this.revision += 1;
      });

      connection.onclose(() => {
        this.connected = false;
        this.connection = null;
        this.startPolling();
      });

      this.connection = connection;

      try {
        await connection.start();
        this.connected = true;
        this.stopPolling();
        await this.refreshUnread();
      } catch {
        // 连不上就退回轮询，功能不中断
        this.connected = false;
        this.connection = null;
        this.startPolling();
      }
    },

    async disconnect() {
      const connection = this.connection;
      this.connection = null;
      this.connected = false;
      if (connection) {
        try {
          await connection.stop();
        } catch {
          // 已断开时忽略
        }
      }
    },

    // 降级轮询：仅在推送不可用时启用
    startPolling(intervalMs = FALLBACK_POLL_INTERVAL) {
      if (this.pollingTimer || this.connected) return;
      this.refreshUnread();
      this.pollingTimer = setInterval(() => this.refreshUnread(), intervalMs);
    },
    stopPolling() {
      if (this.pollingTimer) {
        clearInterval(this.pollingTimer);
        this.pollingTimer = null;
      }
    },

    reset() {
      this.stopPolling();
      this.disconnect();
      this.unread = { notices: 0, inbox: 0, total: 0 };
      this.revision = 0;
    }
  }
});
