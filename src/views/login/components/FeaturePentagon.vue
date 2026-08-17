<template>
  <div class="feature-pentagon">
    <svg viewBox="0 0 400 380" class="pentagon-svg">
      <defs>
        <linearGradient id="lineGrad" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stop-color="#41b883" stop-opacity="0.5" />
          <stop offset="100%" stop-color="#34495e" stop-opacity="0.5" />
        </linearGradient>
        <linearGradient id="centerGrad" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stop-color="#41b883" stop-opacity="0.15" />
          <stop offset="100%" stop-color="#34495e" stop-opacity="0.15" />
        </linearGradient>
      </defs>

      <g class="connect-lines">
        <line v-for="(line, i) in lines" :key="i" :x1="line.x1" :y1="line.y1" :x2="line.x2" :y2="line.y2" />
      </g>

      <g class="center-node">
        <circle cx="200" cy="190" r="50" />
        <circle cx="200" cy="190" r="38" />
        <text x="200" y="198" text-anchor="middle" class="center-text">QPS</text>
      </g>

      <g v-for="(item, i) in features" :key="i" class="feature-node" :transform="`translate(${item.x}, ${item.y})`">
        <circle r="36" />
        <circle r="30" class="node-inner" />
        <foreignObject x="-34" y="-34" width="68" height="68">
          <div class="node-icon">
            <el-icon :size="22" color="#41b883">
              <component :is="item.icon" />
            </el-icon>
          </div>
        </foreignObject>
        <text y="56" text-anchor="middle">{{ item.label }}</text>
      </g>
    </svg>
  </div>
</template>

<script setup lang="ts" name="FeaturePentagon">
import {
  User,
  TrendCharts,
  ChatDotRound,
  OfficeBuilding,
  DataAnalysis
} from "@element-plus/icons-vue";

const features = [
  { label: "客户管理", icon: User, x: 200, y: 50 },
  { label: "商机管理", icon: TrendCharts, x: 78, y: 138 },
  { label: "基地档案", icon: OfficeBuilding, x: 322, y: 138 },
  { label: "跟进记录", icon: ChatDotRound, x: 114, y: 292 },
  { label: "数据分析", icon: DataAnalysis, x: 286, y: 292 }
];

const center = { x: 200, y: 190 };
const lines = features.map(f => ({
  x1: center.x,
  y1: center.y,
  x2: f.x,
  y2: f.y
}));
</script>

<style scoped lang="scss">
.feature-pentagon {
  width: 100%;
  max-width: 320px;

  .pentagon-svg {
    width: 100%;
    height: auto;
    overflow: visible;

    .connect-lines line {
      stroke: url(#lineGrad);
      stroke-width: 1;
      stroke-dasharray: 3 3;
      animation: dash 20s linear infinite;
    }

    @keyframes dash {
      to { stroke-dashoffset: -100; }
    }

    .center-node {
      circle {
        fill: url(#centerGrad);
        stroke: rgba(65, 184, 131, 0.35);
        stroke-width: 1;
      }

      circle:nth-child(2) {
        fill: rgba(65, 184, 131, 0.1);
        stroke: rgba(65, 184, 131, 0.45);
        stroke-width: 1.5;
      }

      .center-text {
        font-size: 20px;
        font-weight: 700;
        fill: #1a6b4a;
        letter-spacing: 2px;
      }
    }

    .feature-node {
      circle {
        fill: rgba(65, 184, 131, 0.06);
        stroke: rgba(65, 184, 131, 0.3);
        stroke-width: 1;
        transition: all 0.3s ease;
      }

      .node-inner {
        fill: rgba(52, 73, 94, 0.05);
        stroke: rgba(52, 73, 94, 0.22);
        stroke-width: 1;
      }

      &:hover circle {
        fill: rgba(65, 184, 131, 0.18);
        stroke: #41b883;
      }

      &:hover .node-inner {
        fill: rgba(52, 73, 94, 0.1);
      }

      .node-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 68px;
        height: 68px;
      }

      text {
        font-size: 13px;
        fill: #375a45;
        font-weight: 500;
        letter-spacing: 1px;
      }
    }
  }
}
</style>