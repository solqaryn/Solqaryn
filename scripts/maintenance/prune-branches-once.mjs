// SOLQARYN: fail-closed, one-time cleanup. Only audited SHA refs may be deleted.
import process from "node:process";
import {setTimeout as sleep} from "node:timers/promises";
const REPO="solqaryn/Solqaryn";
const BASE="7bb329973d0d1cebb862c270fd05d20335893f49";
const QA="5cedcc91bfb2b69562e7093bc1fa3d1251fa31f3";
const PROD="072df831585f879cfd7bb429575d6d387076d97b";
const SOURCE="cleanup/dev-retire-legacy-branches-20261023";
const OPEN=new Set([3723,3695,3687,3604,3583,3582,3581,3580,3578,3577,3574,3554,3549]);
const REQUIRED=["verify-solqaryn-project-scope","SOLQARYN - Seguridad multiempresa","SOLQARYN - Build y pruebas backend"];
const AUDITED=new Set(["9ea03de0c29cca81a4369756a97bffd41f4650b7","4402b1830187016e53c0d63886963d849d527737","f567b7d1d8b5b4530508abebed2900e2930894f2","6f4192da2c8ab023952ef21f6a5f8ba9e3e58224","4fac7aad6fbc1a7d9cc378f1ec1b09befd5e5f29","d89b56022f825158b62d473746a31865ce1e9a65","d280eb7989ea1c1d5e6f978e7cb8561711856df9","fab9e6d22c07d55f26212e6fee2a7ddf419e03e4","82201655abc66e5058968c5c6889e714bb456a63","9e46da0d7bd91f52778b9effab5a6e3277a6daa3","30be7ca100bd29d562f360a4a2ebb8eb75e19401","12db08264e264512166b1f955ca3062f9ecfc1e5","94e99cd32fb75fdfdc17bef2072f79977d68a948","58f6a1c15d6fdf863cfd4e2f8f54b3783652a423","0375e8f88262ddde4ad020cd8d6ece953ae4065d","905c705d273d6f836eaeb70b5b4f1352965c8a00","4389cb04517b98f3064c0932577810518c5cf242","e5d2d81d7bf76297ffa1f8d2d73592d34df90472","8062cff73ebb09449d4ba9a065337037f9a4a6a5","d600e1fa9947e2bb7b4539128012ab47a48c5b5e","74a592c9347cd52e15e3554168c5ca071f579434","8efe1277262cff16b3c49f5c5e329e69969ccd0e","3afd14723616fbea987e97e01e7cafe0d5446bfb","1d6396247837a9c388bc105b6d5975d4baa067e2","66cbaf4e8276741b5c15c559453e1ddc59b37bfe","09e28b97b02b66f7e7b7ae0ab86e01e9b82191da","55bfc5e691bf12d0805eeb009778a0924e59f3c1","d395baaece213368f23a1adf78aedc07425b377a","b2d7eeb95e8d647191b3fe8dfdd49098f0c0acc4","f9561ad902c86bc393d11b27a09e29cbf09ac67a","d0dce2299162f353b36b0ee666f6729a08df25e5","48ccdd910bf926132517f0c93f3121a16b702be0","eed6f364be68a830861668593a36d534f51f776d","e72bb2a3d98d74ec83d90b72f0f85e43207b2d48","7d05a608d5e619d619ea8030da45cc50ef1a87cf","af876ed03ada9715211b97da1f427030ddcd753c","7013a55cd7491c3c79d6808df5db9ba0b8580f40","3ed8e80bf8dcd0f67b0f76afcab24e31c2e5b7ea","a430db828a9306ce9450712fffebd79889cb8dd6","b3c92fbbf319281cd9a73cd74efa6c95c0c1259f","bef39b9d891ca7b654f47440998ed960ee8764b7","235f9d561e13474228a34f24f030dabab89d643e","9049897e974a2067df0d69a05cb23896255d70c3","fd0f5429e3709b1ba8a5bb9c5d8ba4b6db7141a8","963171871e5eebbbd48d522d2ef847bfdffb044b","dee67432a90d654affddc72c28e1ff37ce67b196","ba4edfffcb2473c42d731335cf25dc33121a72ad"]);

const HEAD=process.env.SOLQARYN_COMMIT;
const TOKEN=process.env.GITHUB_TOKEN;
if(!TOKEN||!/^[a-f0-9]{40}$/.test(HEAD??"")||AUDITED.size!==47)throw Error("Invalid cleanup identity or manifest");
async function api(method,route,body){
  const r=await fetch("https://api.github.com/repos/"+REPO+"/"+route,{
    method,headers:{Authorization:"Bearer "+TOKEN,Accept:"application/vnd.github+json",
      "X-GitHub-Api-Version":"2022-11-28",...(body===undefined?{}:{"Content-Type":"application/json"})},
    ...(body===undefined?{}:{body:JSON.stringify(body)})});
  if(!r.ok)throw Error("GitHub "+method+" "+route+" HTTP "+r.status+": "+(await r.text()).slice(0,240));
  return r.status===204?null:r.json();
}
async function refs(){
  const all=[];
  for(let page=1;page<=3;page++){
    const chunk=await api("GET","branches?per_page=100&page="+page);
    all.push(...chunk);
    if(chunk.length<100)return all;
  }
  throw Error("Too many branches");
}
async function prs(){
  const items=await api("GET","pulls?state=open&per_page=100&page=1");
  if(items.length>=100)throw Error("PR listing incomplete");
  return items;
}
const ancestry=await api("GET","git/commits/"+HEAD);
if(ancestry.parents?.[0]?.sha!==BASE)throw Error("Unexpected DEV parent");
const before=await refs();
const lookup=new Map(before.map(x=>[x.name,x]));
if(lookup.size!==before.length)throw Error("Duplicate ref names");
const keep=new Map([["dev",HEAD],["qa",QA],["main",PROD]]);
for(const [name,sha] of keep){
  if(!lookup.get(name)?.protected||lookup.get(name)?.commit.sha!==sha)throw Error("Protected branch drift "+name);
}
const remove=before.filter(x=>!keep.has(x.name)&&x.name!==SOURCE);
if(remove.length!==47||new Set(remove.map(x=>x.commit.sha)).size!==47||
    remove.some(x=>x.protected||!AUDITED.has(x.commit.sha)))throw Error("Unapproved branch drift");
if(lookup.get(SOURCE)?.protected)throw Error("Source PR branch protected");
const open=await prs();
if(open.some(x=>!OPEN.has(x.number)))throw Error("Unknown open PR; no deletion");
console.log("Preflight: 47 audited unprotected refs, 3 protected refs, "+open.length+" old PRs");
for(let t=0;t<36;t++){
  const c=await api("GET","commits/"+HEAD+"/check-runs?per_page=100");
  const states=REQUIRED.map(name=>c.check_runs.find(x=>x.name===name&&x.head_sha===HEAD)?.conclusion);
  if(states.every(x=>x==="success"))break;
  if(states.some(x=>x&&x!=="success"))throw Error("Required CI not green: "+states);
  if(t===35)throw Error("Required CI timeout: "+states);
  await sleep(10000);
}
console.log("Required checks PASS on "+HEAD);
const probe="ops/branch-write-probe-20261023";
if(lookup.has(probe))throw Error("Unexpected branch probe collision");
await api("POST","git/refs",{ref:"refs/heads/"+probe,sha:HEAD});
await api("DELETE","git/refs/heads/"+probe);
console.log("Real GitHub ref create/delete permission verified");
for(const pr of open){
  const result=await api("PATCH","pulls/"+pr.number,{state:"closed"});
  if(result.state!=="closed")throw Error("Cannot close PR #"+pr.number);
  console.log("Closed historical PR #"+pr.number);
}
if((await prs()).length)throw Error("An open PR remains; refusing branch deletion");
for(const ref of remove.sort((x,y)=>x.name.localeCompare(y.name))){
  const current=await api("GET","branches/"+ref.name);
  if(current.protected||current.commit.sha!==ref.commit.sha)throw Error("Ref drift "+ref.name);
  await api("DELETE","git/refs/heads/"+ref.name);
  console.log("Deleted branch "+ref.name);
}
const source=(await refs()).find(x=>x.name===SOURCE);
if(source){
  if(source.protected)throw Error("Source ref unexpectedly protected");
  await api("DELETE","git/refs/heads/"+SOURCE);
}
const after=await refs(),names=after.map(x=>x.name).sort();
if(JSON.stringify(names)!==JSON.stringify(["dev","main","qa"]))throw Error("Postflight refs "+names);
for(const [name,sha] of keep){
  if(after.find(x=>x.name===name)?.commit.sha!==sha)throw Error("Protected SHA changed "+name);
}
console.log("PASS: exactly DEV/QA/MAIN branches remain. No merges of archived commits.");
