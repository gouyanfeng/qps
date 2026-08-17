<template>
  <div class="feature-pentagon">
    <svg viewBox="0 0 400 380" class="pentagon-svg">
      <!-- 连接线 -->
      <g class="connect-lines">
        <line v-for="(line, i) in lines" :key="i" :x1="line.x1" :y1="line.y1" :x2="line.x2" :y2="line.y2" />
      </g>

      <!-- 中心 Logo 圆 -->
      <g class="center-node">
        <circle cx="200" cy="190" r="44" />
        <text x="200" y="198" text-anchor="middle" class="center-text">QPS</text>
      </g>

      <!-- 五个功能节点 -->
      <g v-for="(item, i) in features" :key="i" class="feature-node" :transform="`translate(${item.x}, ${item.y})`">
        <circle r="34" />
        <foreignObject x="-34" y="-34" width="68" height="68">
          <div class="node-icon">
            <el-icon :size="22" color="#14b8a6">
              <component :is="item.icon" />
            </el-icon>
          </div>
        </foreignObject>
        <text y="54" text-anchor="middle">{{ item.label }}</text>
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
  max-width: 420px;

  .pentagon-svg {
    width: 100%;
    height: auto;
    overflow: visible;

    .connect-lines line {
      stroke: rgba(20, 184, 166, 0.35);
      stroke-width: 1.5;
    }

    .center-node {
      circle {
        fill: rgba(20, 184, 166, 0.12);
        stroke: rgba(20, 184, 166, 0.5);
        stroke-width: 1.5;
      }

      .center-text {
        font-size: 18px;
        font-weight: 700;
        fill: #14b8a6;
        letter-spacing: 1px;
      }
    }

    .feature-node {
      circle {
        fill: rgba(20, 184, 166, 0.1);
        stroke: rgba(20, 184, 166, 0.4);
        stroke-width: 1.5;
        transition: all 0.3s ease;
      }

      &:hover circle {
        fill: rgba(20, 184, 166, 0.25);
        stroke: #14b8a6;
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
        fill: #334155;
        font-weight: 500;
      }
    }
  }
}
</style>
