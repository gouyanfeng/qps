<template>
  <section class="entity-card" :style="cardStyle">
    <slot />
    <div v-if="$slots.actions" class="entity-card-actions">
      <slot name="actions" />
    </div>
  </section>
</template>

<script setup lang="ts">
const cardStyle = { flex: "0 0 auto", width: "100%" };
</script>

<style scoped lang="scss">
@import "./EntityCard.scss";
</style>
