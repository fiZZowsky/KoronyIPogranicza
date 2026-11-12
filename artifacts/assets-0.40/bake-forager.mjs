import fs from 'node:fs';import zlib from 'node:zlib';import crypto from 'node:crypto';
import {MeshoptSimplifier as M} from '../assets-0.38/meshopt_simplifier.mjs';await M.ready;
const source=fs.readFileSync('artifacts/assets-0.40/makehuman-base.obj','utf8');let positions=[],uvs=[],faces=[],group='';
for(const line of source.split(/\r?\n/)){let a=line.split(/\s+/);if(a[0]==='v')positions.push(a.slice(1,4).map(Number));if(a[0]==='vt')uvs.push(a.slice(1,3).map(Number));if(a[0]==='g')group=a[1];if(a[0]==='f'&&group==='body'){let f=a.slice(1).map(s=>s.split('/').map(v=>Number(v)-1));for(let i=1;i<f.length-1;i++)faces.push([f[0],f[i],f[i+1]]);}}
let height=16.6589;
positions=positions.map(([x,y,z])=>{let side=x<0?-1:1,dx=Math.abs(x)-1.45,dy=y-4.5;if(Math.abs(x)>1.45&&y>-.4){let w=Math.min(1,dx/.7),angle=-.55*w;x=side*(1.45+dx*Math.cos(angle)-dy*Math.sin(angle));y=4.5+dx*Math.sin(angle)+dy*Math.cos(angle);}return[-x/height,(y+8.1676)/height,-z/height];});
const normals=positions.map(()=>[0,0,0]);const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
for(const f of faces){let p=f.map(v=>positions[v[0]]),a=p[1].map((v,i)=>v-p[0][i]),b=p[2].map((v,i)=>v-p[0][i]),n=cross(a,b);for(const v of f)for(let i=0;i<3;i++)normals[v[0]][i]+=n[i];}
for(const n of normals){let l=Math.hypot(...n)||1;for(let i=0;i<3;i++)n[i]/=l;}
let ps=[],attrs=[],indices=[],used=new Map();for(const f of faces)for(const [p,t] of f){let key=p+'/'+t;if(!used.has(key)){used.set(key,used.size);ps.push(...positions[p]);attrs.push(...normals[p],uvs[t][0],1-uvs[t][1]);}indices.push(used.get(key));}
const p=new Float32Array(ps),a=new Float32Array(attrs),ids=new Uint32Array(indices),bytes=[],lods=[];function u(v){let b=Buffer.alloc(4);b.writeUInt32LE(v);bytes.push(b);}u(0x314d5243);u(2);
for(const target of [12000,1600]){let [ii,error]=M.simplifyWithAttributes(ids,p,3,a,5,[.2,.2,.2,1,1],null,target*3,.04,['Permissive']);let used=new Map(),vs=[],out=[];for(const old of ii){if(!used.has(old)){used.set(old,used.size);vs.push(...p.subarray(old*3,old*3+3),...a.subarray(old*5,old*5+5));}out.push(used.get(old));}u(vs.length/8);u(out.length);bytes.push(Buffer.from(new Float32Array(vs).buffer),Buffer.from(new Uint32Array(out).buffer));lods.push({vertices:vs.length/8,triangles:out.length/3,error});}
fs.writeFileSync('src/Crown.Desktop/Assets/Actors/forager.cmesh',zlib.brotliCompressSync(Buffer.concat(bytes)));
fs.writeFileSync('artifacts/assets-0.40/forager-manifest.json',JSON.stringify({source:'https://raw.githubusercontent.com/makehumancommunity/makehuman/master/makehuman/data/3dobjs/base.obj',sha256:crypto.createHash('sha256').update(source).digest('hex'),license:'CC0-1.0',pose:'offline lowered arms, static',lods},null,2));console.log(lods);
