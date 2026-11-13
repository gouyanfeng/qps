<template>
  <div class="message">
    <el-popover placement="bottom" :width="340" trigger="click" @show="handleShow" @hide="handleHide">
      <template #reference>
        <el-badge :value="noticeStore.unread.total" :hidden="noticeStore.unread.total === 0" :max="99" class="item">
          <i :class="'iconfont icon-xiaoxi'" class="toolBar-icon"></i>
        </el-badge>
      </template>
      <!-- stretch：官方属性，三个 Tab 均分弹层宽度，左中右自然对齐 -->
      <el-tabs v-model="activeName" stretch class="message-tabs" @tab-change="handleTabChange">
        <el-tab-pane :label="`通知(${noticeStore.unread.notices})`" name="notices">
          <div class="message-toolbar">
            <el-button link type="primary" size="small" @click="markAllRead">全部已读</el-button>
          </div>
          <!-- 滚动到底自动续拉下一页；immediate 关闭，避免弹层隐藏时一次性把全部页拉完 -->
          <div
            v-infinite-scroll="() => loadPage('Notices')"
            class="message-scroll"
            :infinite-scroll-disabled="noticeState.loading || !noticeState.hasMore"
            :infinite-scroll-immediate="false"
            :infinite-scroll-distance="40"
          >
            <div v-if="noticeList.length" class="message-list">
              <div
                v-for="item in noticeList"
                :key="item.id"
                class="message-item"
                :class="{ 'is-unread': !item.isRead }"
                @click="handleClick(item)"
              >
                <div class="message-content">
                  <span class="message-title">
                    <el-tag v-if="item.isTop" type="danger" size="small" class="message-tag">置顶</el-tag>
                    {{ item.title }}
                  </span>
                  <span class="message-desc">{{ item.content }}</span>
                  <span class="message-date">{{ formatDate(item.createdAt) }}</span>
                </div>
              </div>
            </div>
            <div v-else-if="!noticeState.loading" class="message-empty">
              <img src="@/assets/images/notData.png" alt="notData" />
              <div>暂无通知</div>
            </div>
            <div v-if="noticeList.length" class="message-loading">
              {{ noticeState.loading ? "加载中…" : noticeState.hasMore ? "上滑加载更多" : "没有更多了" }}
            </div>
          </div>
        </el-tab-pane>
        <el-tab-pane :label="`私信(${noticeStore.unread.inbox})`" name="inbox">
          <div
            v-infinite-scroll="() => loadPage('Inbox')"
            class="message-scroll"
            :infinite-scroll-disabled="inboxState.loading || !inboxState.hasMore"
            :infinite-scroll-immediate="false"
            :infinite-scroll-distance="40"
          >
            <div v-if="inboxList.length" class="message-list">
              <div
                v-for="item in inboxList"
                :key="item.id"
                class="message-item"
                :class="{ 'is-unread': !item.isRead }"
                @click="handleClick(item)"
              >
                <div class="message-content">
                  <span class="message-title">{{ item.title }}</span>
                  <span class="message-desc">{{ item.content }}</span>
                  <span class="message-date">{{ item.senderName || "" }} · {{ formatDate(item.createdAt) }}</span>
                </div>
              </div>
            </div>
            <div v-else-if="!inboxState.loading" class="message-empty">
              <img src="@/assets/images/notData.png" alt="notData" />
              <div>暂无消息</div>
            </div>
            <div v-if="inboxList.length" class="message-loading">
              {{ inboxState.loading ? "加载中…" : inboxState.hasMore ? "上滑加载更多" : "没有更多了" }}
            </div>
          </div>
        </el-tab-pane>
        <el-tab-pane label="待办(0)" name="todo">
          <div class="message-empty">
            <img src="@/assets/images/notData.png" alt="notData" />
            <div>暂无待办</div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-popover>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted, watch } from "vue";
import { ElMessage } from "element-plus";
import { systemMessageApi } from "@/api/modules/system/systemMessage";
import { useNoticeStore } from "@/stores/modules/notice";
import { formatDate } from "@/utils";

type MessageBox = "Notices" | "Inbox";

const PAGE_SIZE = 10;

const noticeStore = useNoticeStore();
const activeName = ref("notices");
const noticeList = ref<any[]>([]);
const inboxList = ref<any[]>([]);
// 每个 Tab 独立分页状态：page 为已加载页码，hasMore 由 totalCount 与已加载量比较得出
const noticeState = reactive({ page: 0, hasMore: true, loading: false });
const inboxState = reactive({ page: 0, hasMore: true, loading: false });
const popoverVisible = ref(false);

const stateOf = (box: MessageBox) => (box === "Notices" ? noticeState : inboxState);
const listOf = (box: MessageBox) => (box === "Notices" ? noticeList : inboxList);

/**
 * 分页加载；reset=true 时回到第一页并替换列表，否则续拉下一页追加。
 * 广播每页会过滤掉当前用户已隐藏的条目，整页被过滤空时继续取下一页（totalCount 兜底）。
 */
const loadPage = async (box: MessageBox, reset = false) => {
  const state = stateOf(box);
  const list = listOf(box);
  if (state.loading || (!reset && !state.hasMore)) return;

  state.loading = true;
  let page = reset ? 1 : state.page + 1;
  try {
    while (true) {
      const res = await systemMessageApi.getMyMessages({ box, page, pageSize: PAGE_SIZE });
      const rows: any[] = res?.data?.list ?? res?.data ?? [];
      const total: number = res?.data?.totalCount ?? rows.length;
      list.value = page === 1 ? rows : [...list.value, ...rows];
      state.page = page;
      state.hasMore = page * PAGE_SIZE < total;
      if (rows.length || !state.hasMore) break;
      page += 1;
    }
  } catch {
    if (reset) list.value = [];
    state.hasMore = false;
  } finally {
    state.loading = false;
  }
};

const handleShow = async () => {
  popoverVisible.value = true;
  await loadCurrentTab();
};

const handleHide = () => {
  popoverVisible.value = false;
};

const handleTabChange = () => {
  void loadCurrentTab();
};

const loadCurrentTab = () => {
  return loadPage(activeName.value === "notices" ? "Notices" : "Inbox", true);
};

const handleClick = async (item: any) => {
  if (!item.isRead) {
    try {
      await systemMessageApi.markRead(item.id);
      item.isRead = true;
      await noticeStore.refreshUnread();
    } catch {}
  }
  ElMessage.info({ message: item.content, duration: 4000, showClose: true });
};

const markAllRead = async () => {
  try {
    await systemMessageApi.markAllRead("Notices");
    noticeList.value.forEach((item: any) => (item.isRead = true));
    await noticeStore.refreshUnread();
    ElMessage.success("全部已读");
  } catch {}
};

// 实时推送到达时，若弹层正打开则静默刷新当前 Tab；
// 已翻到第 2 页之后不重置，避免打断用户阅读（新消息由铃铛红点提示）
watch(
  () => noticeStore.revision,
  () => {
    if (!popoverVisible.value) return;
    const state = stateOf(activeName.value === "notices" ? "Notices" : "Inbox");
    if (state.page > 1) return;
    void loadCurrentTab();
  }
);

onMounted(() => noticeStore.connect());
onUnmounted(() => noticeStore.stopPolling());
</script>

<style scoped lang="scss">
.message-tabs {
  // stretch 已让三个 Tab 均分宽度，这里只收边距、去 item 内边距，保证左中右贴边对齐
  :deep(.el-tabs__header) {
    margin-bottom: 10px;
  }

  :deep(.el-tabs__item) {
    padding: 0;
    text-align: center;
  }

  :deep(.el-tabs__content) {
    padding-top: 0;
  }
}

.message-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 260px;
  line-height: 45px;
}

.message-toolbar {
  display: flex;
  justify-content: flex-end;
  padding: 0 4px 8px;
  border-bottom: 1px solid var(--el-border-color-light);
}

// 列表滚动容器：固定高度内滚动，触底由 v-infinite-scroll 续拉下一页
.message-scroll {
  max-height: 320px;
  overflow-y: auto;
  overscroll-behavior: contain;
}

.message-loading {
  padding: 10px 0;
  text-align: center;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}

.message-list {
  display: flex;
  flex-direction: column;

  .message-item {
    position: relative;
    display: flex;
    align-items: center;
    // 左侧留出未读圆点的位置，已读条目该位置为空，文字仍对齐
    padding: 12px 6px 12px 18px;
    border-bottom: 1px solid var(--el-border-color-light);
    cursor: pointer;
    transition: background-color 0.2s ease;

    // 未读指示点：默认隐藏，未读时显形
    &::before {
      position: absolute;
      top: 16px;
      left: 6px;
      width: 6px;
      height: 6px;
      background-color: var(--el-color-primary);
      border-radius: 50%;
      opacity: 0;
      content: "";
    }

    &:hover {
      background-color: var(--el-fill-color-light);
    }

    &:last-child {
      border: none;
    }

    .message-title {
      color: var(--el-text-color-regular);
    }

    // 未读：圆点 + 主色淡底 + 标题加粗置深，已读保持常规灰
    &.is-unread {
      background-color: var(--el-color-primary-light-9);

      &::before {
        opacity: 1;
      }

      &:hover {
        background-color: var(--el-color-primary-light-8);
      }

      .message-title {
        font-weight: 600;
        color: var(--el-text-color-primary);
      }
    }

    .message-content {
      display: flex;
      flex-direction: column;
      min-width: 0;

      .message-title {
        margin-bottom: 4px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .message-tag {
        margin-right: 6px;
      }

      .message-desc {
        margin-bottom: 4px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        font-size: 12px;
        color: var(--el-text-color-secondary);
      }

      .message-date {
        font-size: 12px;
        color: var(--el-text-color-placeholder);
      }
    }
  }
}
</style>
