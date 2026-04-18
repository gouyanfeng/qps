<script setup lang="ts">

</script>

<template>

    <h1>About QPS</h1>
</template>

<style scoped>
#about {
    padding: 2rem 0;
}

.container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 1rem;
}

h1 {
    font-size: 2.5rem;
    font-weight: 700;
    margin-bottom: 1rem;
    text-align: center;
}

.description {
    font-size: 1.25rem;
    text-align: center;
    margin-bottom: 3rem;
    color: #666;
}

.features,
.tech-stack,
.team {
    margin-bottom: 3rem;
}

h2 {
    font-size: 2rem;
    font-weight: 600;
    margin-bottom: 1.5rem;
    text-align: center;
}

.feature-list {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 2rem;
    margin-top: 2rem;
}

.feature-item {
    background: #f9f9f9;
    padding: 2rem;
    border-radius: 8px;
    text-align: center;
    transition: transform 0.3s ease;
}

.feature-item:hover {
    transform: translateY(-5px);
    box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1);
}

.feature-item .icon {
    width: 48px;
    height: 48px;
    margin-bottom: 1rem;
    color: #4f46e5;
}

.feature-item h3 {
    font-size: 1.25rem;
    font-weight: 600;
    margin-bottom: 0.5rem;
}

.feature-item p {
    color: #666;
}

.stack-list {
    display: flex;
    justify-content: center;
    gap: 2rem;
    flex-wrap: wrap;
    margin-top: 2rem;
}

.stack-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
}

.stack-logo {
    width: 48px;
    height: 48px;
}

.stack-icon {
    width: 48px;
    height: 48px;
    color: #4f46e5;
}

.team p {
    text-align: center;
    color: #666;
    max-width: 800px;
    margin: 0 auto;
}

@media (max-width: 768px) {
    h1 {
        font-size: 2rem;
    }

    h2 {
        font-size: 1.5rem;
    }

    .feature-list {
        grid-template-columns: 1fr;
    }
}
</style>