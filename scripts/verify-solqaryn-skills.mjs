import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

(function buildValidator(){
const root=process.cwd();
const read=(p)=>readFileSync(join(root,p),"utf8").replace(/\r\n/g, "\n");
const skRoot=".agents/skills/solqaryn-project-governance";
const planId="1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA";
const sources=[
["agentskills/agentskills","69ef37e9424c0a7ea9dd2293b559e43ec8176379"],
["pbakaus/impeccable","2149fcce39a90bb409df5f16515f316a76dc6199"],
["emilkowalski/skills","d23d7f88a2e21c9e4b1418c7abe420f5c1052ba7"],
["Leonxlnx/taste-skill","ccbc15639c97057cbfcf32ecebc38ef716e4bb37"],
["blader/humanizer","9862685f575c65a8247f90369951df1b3416e3d6"],
["blader/napkin","27fa60a895de4383b26a539136bc983155cb979c"],
["alexgreensh/token-optimizer","37a9546b9fecba2c4e9a02ef4e90855d449bf08f"],
["JuliusBrussee/caveman","15581d14007fd01fb3f132016741962f34936ca2"],
["angular/angular","6808966993bf2b2d455d9b6245e960a59a01b9a9"],
["openai/skills","49f948faa9258a0c61caceaf225e179651397431"],
["dotnet/skills","9386ed7c1148a7bef970aa0502371c982effa01c"]
];
const snapshot=()=>{
return {skill:read(`${skRoot}/SKILL.md`),architecture:read(`${skRoot}/references/architecture.md`),
routing:read(`${skRoot}/references/skill-routing.md`),meta:read(`${skRoot}/agents/openai.yaml`),
registry:read("docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md"),
allowlist:read("docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md"),agents:read("AGENTS.md"),
automation:read("docs/AUTOMATION_STATUS.md"),frontend:read("frontend/package.json"),
api:read("backend/src/API/Solqaryn.API.csproj"),infra:read("backend/src/Infrastructure/Solqaryn.Infrastructure.csproj"),
dirs:readdirSync(".agents/skills",{withFileTypes:true}).filter(x=>x.isDirectory()).map(x=>x.name)};
};
function validate(x) {
const bad=[];
const expect=(s,k,n)=>{if(!s.includes(k))bad.push(`${n}: missing ${k}`)};
const packageJson=JSON.parse(x.frontend);
const angular=packageJson.dependencies?.["@angular/core"];
const material=packageJson.dependencies?.["@angular/material"];
const tf=x.api.match(/<TargetFramework>([^<]+)<\/TargetFramework>/)?.[1];
const infraTf=x.infra.match(/<TargetFramework>([^<]+)<\/TargetFramework>/)?.[1];
const ef=x.infra.match(/<PackageReference Include="Microsoft\.EntityFrameworkCore" Version="([^"]+)"/)?.[1];
const provider=x.infra.match(/<PackageReference Include="MySql\.EntityFrameworkCore" Version="([^"]+)"/)?.[1];
if(!angular||!tf||!ef||!provider)bad.push("Cannot read code runtime versions");
else {
const dotnet=tf.replace("net","").split(".")[0];
for(const [body,part,name] of [
[x.skill,`Angular ${angular}`,"skill Angular"],[x.architecture,`Angular ${angular}`,"arch Angular"],
[x.skill,`ASP.NET Core ${dotnet}`,"skill .NET"],[x.architecture,`ASP.NET Core ${dotnet}`,"arch .NET"],
[x.registry,`ASP.NET Core ${dotnet}`,"registry .NET"],
[x.skill,`EF Core ${ef}`,"skill EF"],[x.architecture,`EF Core ${ef}`,"arch EF"],
[x.skill,`MySql.EntityFrameworkCore ${provider}`,"skill DB provider"],
[x.architecture,`MySql.EntityFrameworkCore ${provider}`,"arch DB provider"]])expect(body,part,name);
if(angular!==material)bad.push("Angular Material/core versions differ");
if(tf!==infraTf)bad.push("API/Infrastructure target mismatch");
if(x.registry.includes("ASP.NET Core 8"))bad.push("stale registry .NET 8");
}
if(!/^---\nname: solqaryn-project-governance\n/.test(x.skill))bad.push("invalid Skill metadata");
if(!x.meta.includes("display_name: \"SOLQARYN"))bad.push("missing Skill UI metadata");
if(x.dirs.length!==1||x.dirs[0]!=="solqaryn-project-governance")bad.push("local Skill count !=1");
for(const [src,pin] of sources){expect(x.registry,src,"registry origin");expect(x.registry,pin,"registry pin");expect(x.allowlist,src,"allowlist origin");expect(x.allowlist,pin,"allowlist pin");}
for(const [n,body] of [["Skill",x.skill],["AGENTS",x.agents],["AUTOMATION",x.automation]])expect(body,planId,n);
expect(x.skill,"Plan Comercial Inicial Unificado","Skill authority");
expect(x.registry,"EXTERNAL_SKILL_SOURCES=12","registry count");
expect(x.allowlist,"AUTHORIZED_ORIGINAL_SKILL_SOURCES=12","allowlist count");
for(const ref of ["SKREF-10","SKREF-11","SKREF-12"]){
 expect(x.registry,ref,"registry routing id");
 expect(x.allowlist,ref,"allowlist routing id");
 expect(x.routing,ref,"skill routing id");
}
expect(x.routing,"docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md","routing");
expect(x.automation,"AUTOMATION_RUNTIME=RETIRED","automation retirement");
expect(x.automation,"CURRENT_COMMERCIAL_ONLY=TRUE","commercial scope");
expect(x.agents,"PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md","AGENTS path");
return bad;
}
try{
const x=snapshot();
let failures=validate(x);if(failures.length)throw Error(failures.join("\n"));
if(process.argv.includes("--self-test")){
const tests=[
[{...x,skill:x.skill.replaceAll("Angular 22.2.1","Angular 20")},"skill Angular"],
[{...x,registry:x.registry.replace("ASP.NET Core 10","ASP.NET Core 8")},"registry .NET"],
[{...x,skill:x.skill.replace(planId,"INVALID_PLAN")},"Skill"],
[{...x,dirs:[...x.dirs,"rogue-skill"]},"local Skill count"],
[{...x,registry:x.registry.replace("6808966993bf2b2d455d9b6245e960a59a01b9a9","PIN_REMOVED")},"registry pin"],
[{...x,allowlist:x.allowlist.replace("49f948faa9258a0c61caceaf225e179651397431","PIN_REMOVED")},"allowlist pin"],
[{...x,routing:x.routing.replaceAll("SKREF-12","REFERENCE_REMOVED")},"skill routing id"]
];
for(const [broken,needle] of tests){if(!validate(broken).some(x=>x.includes(needle)))throw Error("Negative test not rejected: "+needle);}
console.log(`Skill freshness negative self-tests PASS (${tests.length})`);
}
console.log("Skill freshness PASS: live code versions, commercial plan, one local Skill, 11 pinned GitHub sources plus Skill Creator");
}catch(e){console.error("SOLQARYN SKILL FRESHNESS NO_GO:",e.message);process.exitCode=1;}
})();
