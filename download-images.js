const https = require('https');
const http = require('http');
const fs = require('fs');
const path = require('path');

const outDir = 'e:/Code/QPS/mobile/src/static/images';
fs.mkdirSync(outDir, { recursive: true });

const categories = [
  { key: 'ginseng',        q: 'ginseng root herb photo',                    file: 'herb-ginseng.jpg' },
  { key: 'astragalus',    q: 'astragalus herb slices photo',                file: 'herb-astragalus.jpg' },
  { key: 'danggui',       q: 'angelica sinensis root herb photo',           file: 'herb-danggui.jpg' },
  { key: 'baizhu',        q: 'atractylodes macrocephala herb photo',        file: 'herb-baizhu.jpg' },
  { key: 'gancao',        q: 'licorice root herb gan cao photo',            file: 'herb-gancao.jpg' },
  { key: 'chrysanthemum', q: 'dried chrysanthemum flowers tea photo',       file: 'herb-chrysanthemum.jpg' },
  { key: 'goji',          q: 'dried goji berries wolfberry photo',          file: 'herb-goji.jpg' },
  { key: 'chenpi',        q: 'dried tangerine peel chenpi herb photo',      file: 'herb-chenpi.jpg' },
  { key: 'rose',          q: 'dried rose petals herbal tea photo',          file: 'herb-rose.jpg' },
  { key: 'ejiao',         q: 'donkey hide gelatin ejiao photo',             file: 'herb-ejiao.jpg' },
  { key: 'gypsum',        q: 'gypsum mineral crystal photo',                file: 'herb-gypsum.jpg' },
  { key: 'banner1',       q: 'chinese herbal medicine assorted wooden table',file: 'banner-001.jpg' },
  { key: 'banner2',       q: 'dried herbs tea leaves rustic wood top view', file: 'banner-002.jpg' },
  { key: 'banner3',       q: 'traditional medicine ingredients flat lay',   file: 'banner-003.jpg' },
];

const UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36';
const goodHosts = /images\.unsplash\.com|images\.pexels\.com|cdn\.pixabay\.com|cdn\.shopify\.com|upload\.wikimedia\.org|media\.istockphoto\.com[^/]*\/photo-/;
const badHosts = /shutterstock|alamy|dreamstime|gettyimages|istockphoto|depositphotos|123rf|freepik|canva|adobe|vecteezy|istockphoto\.com\/photo|linkedin|facebook|tiktok|pinterest|reddit|youtube|wikipedia\.org\/en/;

function log(msg) { process.stdout.write(msg + '\n'); }

function bingSearch(query) {
  return new Promise((resolve) => {
    const url = 'https://www.bing.com/images/search?q=' + encodeURIComponent(query) + '&form=HDRSC3&first=1';
    const req = https.get(url, { headers: { 'User-Agent': UA } }, (r) => {
      let d = '';
      r.on('data', (c) => (d += c));
      r.on('end', () => {
        const re = /murl&quot;:&quot;(https?:[^&]+)/g;
        const m = [];
        let x;
        while ((x = re.exec(d)) !== null) {
          m.push(decodeURIComponent(x[1].replace(/&amp;/g, '&')));
        }
        resolve([...new Set(m)]);
      });
    });
    req.on('error', () => resolve([]));
    req.setTimeout(15000, () => { req.destroy(); resolve([]); });
  });
}

function download(url, target) {
  return new Promise((resolve) => {
    const h = url.startsWith('https') ? https : http;
    const req = h.get(url, { headers: { 'User-Agent': UA, 'Accept': 'image/*,*/*' } }, (r) => {
      if (r.statusCode >= 300 && r.statusCode < 400 && r.headers.location) {
        download(r.headers.location, target).then(resolve); return;
      }
      if (r.statusCode >= 400) { log('    FAIL ' + r.statusCode); resolve(false); return; }
      const ct = r.headers['content-type'] || '';
      if (!ct.startsWith('image/')) { log('    NOT IMAGE (' + ct.slice(0, 20) + ')'); resolve(false); return; }
      const w = fs.createWriteStream(target);
      r.pipe(w);
      w.on('finish', () => {
        w.close();
        const sz = fs.statSync(target).size;
        if (sz > 5000) { log('    OK  ' + (sz/1024|0) + 'KB  ' + url.split('/')[2]); resolve(true); }
        else { log('    TOO SMALL (' + sz + ')'); try { fs.unlinkSync(target); } catch (e) {} resolve(false); }
      });
    });
    req.on('error', (e) => { log('    FAIL: ' + e.message); resolve(false); });
    req.setTimeout(20000, () => { req.destroy(); resolve(false); });
  });
}

async function run() {
  for (const cat of categories) {
    log('\n>>> ' + cat.key + ' (' + cat.q + ')');
    const allUrls = await bingSearch(cat.q);
    const urls = allUrls.filter(u => goodHosts.test(u) && !badHosts.test(u));
    log('  Got ' + allUrls.length + ' total, ' + urls.length + ' from good hosts');
    let ok = false;
    for (const u of urls) {
      log('  Try: ' + u.slice(0, 100));
      ok = await download(u, path.join(outDir, cat.file));
      if (ok) break;
    }
    if (!ok && urls.length === 0) {
      log('  No good-host URLs, trying all non-stock URLs...');
      const fallback = allUrls.filter(u => !badHosts.test(u));
      for (const u of fallback) {
        log('  Try: ' + u.slice(0, 100));
        ok = await download(u, path.join(outDir, cat.file));
        if (ok) break;
      }
    }
    if (!ok) log('  *** ALL FAILED ***');
  }
  log('\n=== ALL DONE ===');
}
run();