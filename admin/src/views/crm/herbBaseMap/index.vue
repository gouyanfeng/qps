<template>
  <div class="herb-base-map-page">
    <div ref="mapContainer" class="map-container"></div>

    <div class="panel">
      <div class="panel-header">
        <div class="panel-title">基地地图</div>
        <div class="panel-stats">
          <el-tag size="small" type="primary">共 {{ bases.length }} 个基地</el-tag>
        </div>
      </div>

      <div class="panel-search">
        <el-input
          v-model="keyword"
          placeholder="搜索基地名称 / 地址"
          clearable
          :prefix-icon="Search"
          size="default"
        />
      </div>

      <div class="panel-list" v-loading="loading">
        <div
          v-for="b in filteredBases"
          :key="b.id"
          class="base-item"
          :class="{ active: activeId === b.id }"
          @click="focusBase(b)"
        >
          <div class="base-item-name">{{ b.baseName || "未命名基地" }}</div>
          <div class="base-item-meta">{{ b.address || "暂无地址" }}</div>
        </div>
        <el-empty v-if="!filteredBases.length" description="没有匹配的基地" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { Search } from "@element-plus/icons-vue";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import "leaflet.markercluster/dist/MarkerCluster.css";
import crmHerbBaseApi from "@/api/modules/crm/crmHerbBase";

interface HerbBaseMapItem {
  id: string;
  baseName: string;
  address: string;
  lat: number;
  lng: number;
}

const mapContainer = ref<HTMLElement | null>(null);
const loading = ref(false);
const keyword = ref("");
const activeId = ref<string | null>(null);

const bases = ref<HerbBaseMapItem[]>([]);
let map: L.Map | null = null;
let markerLayer: L.MarkerClusterGroup | null = null;
const markerIndex = new Map<string, L.Marker>();

const filteredBases = computed(() => {
  const kw = keyword.value.trim().toLowerCase();
  if (!kw) return bases.value;
  return bases.value.filter(b => {
    const hay = `${b.baseName || ""} ${b.address || ""}`.toLowerCase();
    return hay.includes(kw);
  });
});

const clusterIcon = (cluster: L.MarkerCluster) => {
  const count = cluster.getChildCount();
  let level = "sm";
  if (count >= 800) level = "lg";
  else if (count >= 100) level = "md";
  return L.divIcon({
    className: "qb-cluster-icon",
    html: `<div class="qb-cluster qb-cluster-${level}"><span>${count}</span></div>`,
    iconSize: L.point(40, 40),
  });
};

let clusterPluginReady = false;
const ensureClusterPlugin = async () => {
  if (clusterPluginReady) return;
  (window as any).L = L;
  await import("leaflet.markercluster");
  clusterPluginReady = true;
};

const initMap = async () => {
  if (!mapContainer.value || map) return;

  await ensureClusterPlugin();

  map = L.map(mapContainer.value, {
    center: [34.3, 108.9],
    zoom: 5,
    zoomControl: true,
    attributionControl: true,
  });

  L.tileLayer(
    "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
    {
      attribution: "Tiles &copy; Esri — Source: Esri, Maxar, Earthstar Geographics",
      maxNativeZoom: 17,
      maxZoom: 19,
    }
  ).addTo(map);

  markerLayer = L.markerClusterGroup({
    showCoverageOnHover: false,
    spiderfyOnMaxZoom: true,
    chunkedLoading: true,
    chunkInterval: 200,
    chunkDelay: 50,
    maxClusterRadius: (zoom: number) => {
      if (zoom <= 5) return 200;
      if (zoom <= 7) return 150;
      if (zoom <= 9) return 110;
      if (zoom <= 11) return 80;
      if (zoom <= 13) return 60;
      return 45;
    },
    disableClusteringAtZoom: 16,
    iconCreateFunction: clusterIcon,
  }).addTo(map);
};

const redrawMarkers = () => {
  if (!map || !markerLayer) return;
  markerLayer.clearLayers();
  markerIndex.clear();

  const bounds: L.LatLngTuple[] = [];
  const markers: L.Marker[] = [];

  bases.value.forEach(b => {
    const latLng: L.LatLngTuple = [b.lat, b.lng];
    bounds.push(latLng);

    const icon = L.divIcon({
      className: "qb-base-marker",
      html: `<div class="qb-marker-dot"></div>`,
      iconSize: [20, 20],
      iconAnchor: [10, 10],
    });

    const marker = L.marker(latLng, { icon });

    const popupContent = `
      <div class="qb-popup">
        <div class="qb-popup-title">${b.baseName || "未命名基地"}</div>
        <div class="qb-popup-row">📍 ${b.address || "暂无地址"}</div>
        <div class="qb-popup-coord">坐标：${b.lat.toFixed(5)}, ${b.lng.toFixed(5)}</div>
      </div>
    `;

    marker.bindPopup(popupContent);
    marker.on("click", () => {
      activeId.value = b.id;
    });

    markerIndex.set(b.id, marker);
    markers.push(marker);
  });

  markerLayer.addLayers(markers);

  if (bounds.length > 0) {
    map.fitBounds(bounds, { padding: [40, 40], maxZoom: 12 });
  }
};

const focusBase = (b: HerbBaseMapItem) => {
  activeId.value = b.id;
  if (!map || !markerLayer) return;

  const marker = markerIndex.get(b.id);
  if (marker) {
    markerLayer.zoomToShowLayer(marker, () => {
      marker.openPopup();
    });
    return;
  }

  map.flyTo([b.lat, b.lng], 13, { duration: 0.8 });
};

const loadBases = async () => {
  loading.value = true;
  try {
    const result = await crmHerbBaseApi.getHerbBaseMap();
    const list: any[] = Array.isArray(result) ? result : (result.data || []);
    bases.value = list.map((r: any) => ({
      id: r.id,
      baseName: r.baseName,
      address: r.address,
      lat: Number(r.lat),
      lng: Number(r.lng),
    }));
  } catch (e) {
    console.error("加载基地失败", e);
  } finally {
    loading.value = false;
    redrawMarkers();
  }
};

onMounted(async () => {
  await initMap();
  await loadBases();
});

onBeforeUnmount(() => {
  if (map) {
    map.remove();
    map = null;
  }
});
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>

<style lang="scss">
@import "./index-second.scss";
</style>