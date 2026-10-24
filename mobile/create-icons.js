const fs = require('fs');
const path = require('path');

const tabbarDir = path.join(__dirname, 'src', 'static', 'tabbar');
if (!fs.existsSync(tabbarDir)) {
  fs.mkdirSync(tabbarDir, { recursive: true });
}

const icons = [
  'home.png', 'home-active.png',
  'category.png', 'category-active.png',
  'cart.png', 'cart-active.png',
  'search.png', 'search-active.png'
];

const base64Png = 'iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAMElEQVRYR+3BAQEAAACCgB/9v20JJWwFTygAAAAAAAAAAAAAAAAAAAAAAAAAAADwDQ+GAQF/9fX4AAAAAElFTkSuQmCC';
const buffer = Buffer.from(base64Png, 'base64');

icons.forEach(icon => {
  fs.writeFileSync(path.join(tabbarDir, icon), buffer);
  console.log(`Created ${icon}`);
});

console.log('All icons created successfully');