import urllib.request,io,zipfile,json,hashlib
from pathlib import Path
url='https://files2.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip'
class Remote(io.RawIOBase):
 def __init__(self):
  r=urllib.request.urlopen(urllib.request.Request(url,headers={'Range':'bytes=0-0'}),timeout=30)
  assert r.status==206,'Range required';self.size=int(r.headers['Content-Range'].split('/')[-1]);self.pos=0;r.close()
 def seekable(self):return True
 def seek(self,p,w=0):self.pos=p if w==0 else self.pos+p if w==1 else self.size+p;return self.pos
 def tell(self):return self.pos
 def read(self,n=-1):
  if n<0:n=self.size-self.pos
  if n==0:return b''
  end=min(self.size-1,self.pos+n-1)
  r=urllib.request.urlopen(urllib.request.Request(url,headers={'Range':f'bytes={self.pos}-{end}'}),timeout=45)
  assert r.status==206
  data=r.read();self.pos+=len(data);return data
p=Path('artifacts/assets-0.40/makehuman');p.mkdir(exist_ok=True)
z=zipfile.ZipFile(Remote());names=z.namelist();Path('artifacts/assets-0.40/pack-files.json').write_text(json.dumps(names))
selected=[n for n in names if ('young_caucasian_male/' in n or '/short01/' in n or '/low-poly/' in n) and n.endswith(('.png','.obj','.mhmat','.mhclo'))]
records=[]
for n in selected:
 data=z.read(n);target=p/Path(n).name;target.write_bytes(data);records.append(dict(path=n,local=str(target),sha256=hashlib.sha256(data).hexdigest()));print(n,len(data),flush=True)
Path('artifacts/assets-0.40/source-manifest.json').write_text(json.dumps(dict(url=url,license='CC0-1.0',files=records),indent=2))
