import { ElLoading } from "element-plus";

/* 全局请求 loading */
let loadingInstance: ReturnType<typeof ElLoading.service>;

/**
 * @description 开启 Loading
 * */
const startLoading = () => {
  // 遮罩只盖内容区（el-main），不全屏挂 body：
  // - fullscreen/lock 会在 body 上加 el-loading-parent--hidden（overflow:hidden），
  //   切换菜单时滚动条消失/出现导致整页布局跳动，观感为"页面闪一下"
  // - target 指向 el-main 后，relative/hidden class 加在 el-main 上，body 不受影响
  const target = document.querySelector(".el-main") || document.body;
  loadingInstance = ElLoading.service({
    target,
    lock: false,
    text: "Loading",
    background: "var(--el-mask-color)"
  });
};

/**
 * @description 结束 Loading
 * */
const endLoading = () => {
  loadingInstance.close();
};

/**
 * @description 显示全屏加载
 * */
let needLoadingRequestCount = 0;
export const showFullScreenLoading = () => {
  if (needLoadingRequestCount === 0) {
    startLoading();
  }
  needLoadingRequestCount++;
};

/**
 * @description 隐藏全屏加载
 * */
export const tryHideFullScreenLoading = () => {
  if (needLoadingRequestCount <= 0) return;
  needLoadingRequestCount--;
  if (needLoadingRequestCount === 0) {
    endLoading();
  }
};


