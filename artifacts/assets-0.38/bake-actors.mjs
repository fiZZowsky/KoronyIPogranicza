// Offline asset preparation. meshoptimizer is MIT; source models are CC0 (see manifest).
import fs from 'node:fs';
import zlib from 'node:zlib';
import crypto from 'node:crypto';
import {MeshoptSimplifier as M} from './meshopt_simplifier.mjs';
await M.ready;
const out='src/Crown.Desktop/Assets/Actors';fs.mkdirSync(out,{recursive:true});
const I=()=>[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1];
function mul(a,b){let c=Array(16).fill(0);for(let i=0;i<4;i++)for(let j=0;j<4;j++)for(let k=0;k<4;k++)c[j*4+i]+=a[k*4+i]*b[j*4+k];return c;}
function trs(n){if(n.matrix)return n.matrix;let [x,y,z,w]=n.rotation??[0,0,0,1],[sx,sy,sz]=n.scale??[1,1,1],[tx,ty,tz]=n.translation??[0,0,0];return[(1-2*y*y-2*z*z)*sx,(2*x*y+2*z*w)*sx,(2*x*z-2*y*w)*sx,0,(2*x*y-2*z*w)*sy,(1-2*x*x-2*z*z)*sy,(2*y*z+2*x*w)*sy,0,(2*x*z+2*y*w)*sz,(2*y*z-2*x*w)*sz,(1-2*x*x-2*y*y)*sz,0,tx,ty,tz,1];}
function transform(m,p,normal=false){return [0,1,2].map(r=>m[r]*p[0]+m[r+4]*p[1]+m[r+8]*p[2]+(normal?0:m[r+12]));}
const selected=process.argv.slice(2);const previous=JSON.parse(fs.readFileSync('artifacts/assets-0.38/manifest.json','utf8'));const manifest=[];
for(const name of (selected.length?selected:['horse','viking','knight','modern','soldier','civilian','robot','viking-rider','knight-rider'])){
 const source=fs.readFileSync(`artifacts/assets-0.38/${name.replace("-rider","")}-source.glb`),len=source.readUInt32LE(12),g=JSON.parse(source.subarray(20,20+len)),bin=source.subarray(28+len);
 function read(id){const a=g.accessors[id],v=g.bufferViews[a.bufferView],n=({SCALAR:1,VEC2:2,VEC3:3,VEC4:4,MAT4:16})[a.type],size=({5121:1,5123:2,5125:4,5126:4})[a.componentType];if(!n||!size||a.sparse)throw Error('Unsupported accessor');let data=[];for(let i=0;i<a.count;i++)for(let j=0;j<n;j++){let p=(v.byteOffset??0)+(a.byteOffset??0)+i*(v.byteStride??n*size)+j*size;data.push(a.componentType==5126?bin.readFloatLE(p):a.componentType==5125?bin.readUInt32LE(p):a.componentType==5123?bin.readUInt16LE(p):bin.readUInt8(p));}return data;}
 const nodes=structuredClone(g.nodes),parents=[];nodes.forEach((n,i)=>(n.children??[]).forEach(c=>parents[c]=i));
 // Bake a balanced locomotion pose, rather than presenting an A-pose mannequin.
 const animation=g.animations?.find(a=>a.name==='walk');
 if(animation)for(const channel of animation.channels){const sampler=animation.samplers[channel.sampler],times=read(sampler.input),values=read(sampler.output),count=channel.target.path==='rotation'?4:3;let t=times.at(-1)*.17,k=0;while(k+1<times.length&&times[k+1]<t)k++;let next=Math.min(k+1,times.length-1),f=(t-times[k])/Math.max(.00001,times[next]-times[k]);let a=values.slice(k*count,k*count+count),b=values.slice(next*count,next*count+count);if(count===4&&a.reduce((s,v,i)=>s+v*b[i],0)<0)b=b.map(v=>-v);let value=a.map((v,i)=>v+(b[i]-v)*f);if(count===4){let l=Math.hypot(...value);value=value.map(v=>v/l);}nodes[channel.target.node][channel.target.path]=value;}
 const worlds=[];function world(i){return worlds[i]??=(parents[i]===undefined?trs(nodes[i]):mul(world(parents[i]),trs(nodes[i])));}
 const nodeId=nodes.findIndex(n=>n.mesh!==undefined),node=nodes[nodeId],primitive=g.meshes[node.mesh].primitives[0];
 if(g.meshes[node.mesh].primitives.length!==1)throw Error('Expected a single material primitive');
 let positions=read(primitive.attributes.POSITION),normals=read(primitive.attributes.NORMAL),uv=read(primitive.attributes.TEXCOORD_0),indices=new Uint32Array(read(primitive.indices));
 if(node.skin!==undefined){const skin=g.skins[node.skin],ib=read(skin.inverseBindMatrices),matrices=skin.joints.map((j,i)=>mul(world(j),ib.slice(i*16,i*16+16))),joints=read(primitive.attributes.JOINTS_0),weights=read(primitive.attributes.WEIGHTS_0);for(let i=0;i<positions.length/3;i++){let p=[0,0,0],n=[0,0,0];for(let j=0;j<4;j++){let w=weights[i*4+j];if(!w)continue;let m=matrices[joints[i*4+j]],pp=transform(m,positions.slice(i*3,i*3+3)),nn=transform(m,normals.slice(i*3,i*3+3),true);for(let k=0;k<3;k++){p[k]+=pp[k]*w;n[k]+=nn[k]*w;}}let l=Math.hypot(...n);for(let k=0;k<3;k++){positions[i*3+k]=p[k];normals[i*3+k]=n[k]/l;}}}
 else for(let i=0;i<positions.length/3;i++){let p=transform(world(nodeId),positions.slice(i*3,i*3+3)),n=transform(world(nodeId),normals.slice(i*3,i*3+3),true);for(let k=0;k<3;k++){positions[i*3+k]=p[k];normals[i*3+k]=n[k];}}
 const min=[Infinity,Infinity,Infinity],max=[-Infinity,-Infinity,-Infinity];for(let i=0;i<positions.length;i++){min[i%3]=Math.min(min[i%3],positions[i]);max[i%3]=Math.max(max[i%3],positions[i]);}
 let h=max[1]-min[1];for(let i=0;i<positions.length/3;i++){positions[i*3]=-(positions[i*3]-(min[0]+max[0])/2)/h;positions[i*3+1]=(positions[i*3+1]-min[1])/h;positions[i*3+2]=-(positions[i*3+2]-(min[2]+max[2])/2)/h;normals[i*3]*=-1;normals[i*3+2]*=-1;}
 if(name.endsWith('-rider'))for(let i=0;i<positions.length/3;i++){
  let y=positions[i*3+1],side=positions[i*3]<0?-1:1;if(y>=.52)continue;
  let d=Math.min(.22,.52-y);positions[i*3]+=side*d*.8;positions[i*3+2]+=d*.6;
  positions[i*3+1]=y>=.30?.52-(.52-y)*.35:.443-(.30-y);
  if(y>=.30){let nx=normals[i*3],ny=normals[i*3+1],nz=normals[i*3+2];ny=(ny+.8*side*nx+.6*nz)/.35;let l=Math.hypot(nx,ny,nz);normals[i*3]=nx/l;normals[i*3+1]=ny/l;normals[i*3+2]=nz/l;}
 }
 const p=new Float32Array(positions),attrs=new Float32Array(positions.length/3*5);for(let i=0;i<p.length/3;i++)attrs.set([...normals.slice(i*3,i*3+3),...uv.slice(i*2,i*2+2)],i*5);
 const counts=[];const bytes=[];function u(v){let b=Buffer.alloc(4);b.writeUInt32LE(v);bytes.push(b);}u(0x314D5243);u(2);
 for(const target of [name==='horse'?10000:8000,name==='horse'?1400:1000]){
  let [ii,error]=M.simplifyWithAttributes(indices,p,3,attrs,5,[.2,.2,.2,1,1],null,target*3,.08,['Permissive']);
  const used=new Map(),vs=[],ids=[];for(const old of ii){if(!used.has(old)){used.set(old,used.size);vs.push(...p.subarray(old*3,old*3+3),...attrs.subarray(old*5,old*5+5));}ids.push(used.get(old));}
  u(vs.length/8);u(ids.length);bytes.push(Buffer.from(new Float32Array(vs).buffer),Buffer.from(new Uint32Array(ids).buffer));counts.push({triangles:ids.length/3,vertices:vs.length/8,error});
 }
 fs.writeFileSync(`${out}/${name}.cmesh`,zlib.brotliCompressSync(Buffer.concat(bytes)));
 const material=g.materials[primitive.material??0],pbr=material.pbrMetallicRoughness;
 if(!name.endsWith('-rider'))for(const [suffix,id] of [['base',pbr.baseColorTexture.index],['normal',material.normalTexture.index],['orm',pbr.metallicRoughnessTexture.index]]){let image=g.images[g.textures[id].source],view=g.bufferViews[image.bufferView];if(image.mimeType!=='image/jpeg')throw Error('Texture encoding changed');fs.writeFileSync(`${out}/${name}-${suffix}.jpg`,bin.subarray(view.byteOffset??0,(view.byteOffset??0)+view.byteLength));}
 manifest.push({name,sourceSHA256:crypto.createHash('sha256').update(source).digest('hex'),sourceTriangles:indices.length/3,pose:animation?'walk@17%':'static',lods:counts,sourceLicense:'CC0-1.0',toolLicense:'MIT',normalizationHeight:1});console.log(name,counts);
}
fs.writeFileSync('artifacts/assets-0.38/manifest.json',JSON.stringify(selected.length?previous.map(row=>manifest.find(m=>m.name===row.name)??row):manifest,null,2));
