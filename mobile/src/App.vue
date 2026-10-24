<script setup lang="ts">
import { onLaunch, onShow, onHide } from "@dcloudio/uni-app";
onLaunch(() => {
  console.log("App Launch");
});
onShow(() => {
  console.log("App Show");
});
onHide(() => {
  console.log("App Hide");
});
</script>

<style lang="scss">
@use "./icons/iconfont.scss";

/*
 * QPS 移动端全局皮肤类。
 * 变量定义见 uni.scss（uni-app 已全局注入），这里只做落地——
 * 页面直接挂类名即可复用，避免每页重复写死颜色、阴影、圆角和间距。
 */

/* 屏蔽 uni-app 默认从 cdn.dcloud.net.cn 预加载的 tabBar 阴影图，避免外网请求。 */
body:after {
  background-image: none !important;
  animation: none !important;
}

/*
 * 顶部区域统一尺寸：所有页面顶部栏共用同一套变量，
 * 由 q-page-header / q-page-header-back 落地，避免各页各写一份 padding 导致高度不一致。
 */
:root,
page {
  --page-bg: #FCF9F8;
  --safe-top: max(88rpx, env(safe-area-inset-top));
  --topbar-height: 62rpx;
  --topbar-padding-x: 22rpx;
  --topbar-padding-bottom: 16rpx;
  --page-top-offset: calc(var(--safe-top) + var(--topbar-height) + var(--topbar-padding-bottom));
}

/* 页面底色统一，避免页面切换时露白。 */
page {
  background-color: var(--page-bg);
}

/* 页面根背景。 */
.qps-page {
  min-height: 100vh;
  background: var(--page-bg);
}

/* 通用卡片：白底 + 圆角 + 柔和阴影。 */
.qps-card {
  background: $theme-card-bg;
  border-radius: 24rpx;
  box-shadow: $theme-shadow;
}

/* 轻阴影（列表项、小徽章用）。 */
.qps-shadow-sm {
  box-shadow: 0 4rpx 12rpx rgba(46, 125, 50, 0.06);
}

/* 页面顶部 Hero 渐变容器。 */
.qps-hero {
  background: linear-gradient(135deg, $theme-primary 0%, $theme-primary-light 100%);
}

/* 分组标题（带图标或装饰线）。 */
.qps-section-title {
  font-size: 36rpx;
  font-weight: bold;
  color: $theme-primary-dark;
  letter-spacing: 1rpx;
}

/* 辅助文案（查看更多、说明等）。 */
.qps-section-more {
  font-size: 24rpx;
  color: $theme-primary-light;
  font-weight: 500;
}

/* 价格强调色（统一使用主题橙，不再混用红色）。 */
.qps-price-accent {
  color: $theme-accent;
}

.qps-price-symbol {
  font-size: 24rpx;
  font-weight: bold;
}

.qps-price-value {
  font-size: 36rpx;
  font-weight: bold;
  margin-left: 4rpx;
}

/* 搜索栏（胶囊 + 半透明白）。 */
.qps-search-bar {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 20rpx 30rpx;
  background: rgba(255, 255, 255, 0.95);
  border-radius: 50rpx;
  box-shadow: 0 4rpx 12rpx rgba(0, 0, 0, 0.1);
}

/* 空态提示。 */
.qps-empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 120rpx 0;
  color: $theme-text-hint;
  font-size: 26rpx;
}

/* 点击态统一效果。 */
.qps-tap {
  transition: all 0.3s ease;

  &:active {
    transform: scale(0.98);
    opacity: 0.9;
  }
}
</style>
