"""Renders every SVG of Manifest.json to its own PNG (transparent background) at the `scale` of the manifest.

Rasterized by headless Edge: the SVG gets its final pixel size (width/height * scale, viewBox unchanged), so the vector is
drawn at full resolution and never scaled up from a bitmap.
Usage: python RenderPngs.py [Name ...] [--manifest=File.json] [--out=Folder]   (no arguments: everything)
"""
import json
import subprocess
import sys
import tempfile
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse, parse_qs

ROOT = Path(__file__).resolve().parent.parent
EDGE = r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
args = [a for a in sys.argv[1:] if not a.startswith('--')]
opts = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--') and '=' in a)
OUT = opts.get('out', 'Png')
manifest = json.loads((ROOT / opts.get('manifest', 'Manifest.json')).read_text(encoding='utf-8'))
only = set(args)
if only:
    manifest = [m for m in manifest if m['name'] in only]

PAGE = """<!doctype html><meta charset="utf-8"><body><script>
const log = (m) => fetch('/log', {method: 'POST', body: m});
(async () => {
  const items = await (await fetch('/items.json')).json();
  for (const it of items) {
    try {
      let txt = await (await fetch('/Svg/' + it.category + '/' + it.name + '.svg')).text();
      const W = Math.round(it.w * it.scale), H = Math.round(it.h * it.scale);
      txt = txt.replace(/^(<svg[^>]*?)width="[^"]*"\\s+height="[^"]*"/, '$1width="' + W + '" height="' + H + '"');
      const url = URL.createObjectURL(new Blob([txt], {type: 'image/svg+xml'}));
      const img = new Image();
      img.src = url;
      await img.decode();
      const c = document.createElement('canvas');
      c.width = W; c.height = H;
      const g = c.getContext('2d');
      g.clearRect(0, 0, W, H);
      g.drawImage(img, 0, 0, W, H);
      const blob = await new Promise(r => c.toBlob(r, 'image/png'));
      await fetch('/save?path=' + encodeURIComponent('__OUT__/' + it.category + '/' + it.name + '.png'), {method: 'POST', body: blob});
    } catch (e) { await log('ERR ' + it.name + ' ' + e); }
  }
  await fetch('/done', {method: 'POST', body: ''});
})();
</script>"""

done = threading.Event()


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def _send(self, body, ctype='text/plain'):
        self.send_response(200)
        self.send_header('Content-Type', ctype)
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        p = urlparse(self.path).path
        if p == '/render.html':
            return self._send(PAGE.replace('__OUT__', OUT).encode('utf-8'), 'text/html; charset=utf-8')
        if p == '/items.json':
            return self._send(json.dumps(manifest).encode('utf-8'), 'application/json')
        f = (ROOT / p.lstrip('/')).resolve()
        if ROOT in f.parents and f.is_file():
            ct = 'image/svg+xml' if f.suffix == '.svg' else 'application/octet-stream'
            return self._send(f.read_bytes(), ct)
        self.send_response(404)
        self.end_headers()

    def do_POST(self):
        u = urlparse(self.path)
        n = int(self.headers.get('Content-Length', 0))
        data = self.rfile.read(n)
        if u.path == '/save':
            rel = parse_qs(u.query)['path'][0]
            out = (ROOT / rel).resolve()
            assert ROOT in out.parents
            out.parent.mkdir(parents=True, exist_ok=True)
            out.write_bytes(data)
        elif u.path == '/log':
            print(data.decode('utf-8', 'replace'))
        elif u.path == '/done':
            done.set()
        self._send(b'ok')


server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
port = server.server_address[1]
threading.Thread(target=server.serve_forever, daemon=True).start()
profile = tempfile.mkdtemp(prefix='edge-render-')
proc = subprocess.Popen([EDGE, '--headless=new', '--disable-gpu', f'--user-data-dir={profile}', '--no-first-run',
                         f'http://127.0.0.1:{port}/render.html'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
start = time.time()
ok = done.wait(240)
proc.terminate()
server.shutdown()
print(('OK' if ok else 'TIMEOUT'), len(manifest), 'files,', round(time.time() - start, 1), 's')
