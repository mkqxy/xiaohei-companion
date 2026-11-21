/* Offline companion editor. No requests, eval, telemetry or executable actions. */
"use strict";
const STATES = Object.freeze([
 {id:"idle",label:"安静待机",row:0,ms:[1680,660,660,840,840,1920]},
 {id:"running-right",label:"向右跑",row:1,ms:[120,120,120,120,120,120,120,220]},
 {id:"running-left",label:"向左跑",row:2,ms:[120,120,120,120,120,120,120,220]},
 {id:"waving",label:"招招手",row:3,ms:[140,140,140,280]},
 {id:"jumping",label:"小小跳跃",row:4,ms:[140,140,140,140,280]},
 {id:"failed",label:"有点失落",row:5,ms:[140,140,140,140,140,140,140,240]},
 {id:"waiting",label:"等你回应",row:6,ms:[150,150,150,150,150,260]},
 {id:"running",label:"专注工作",row:7,ms:[120,120,120,120,120,220]},
 {id:"review",label:"认真检查",row:8,ms:[150,150,150,150,150,280]}
]);
const DEFAULT = {schemaVersion:1,name:"小黑和嘿咻",greeting:"你忙你的，我在这里陪你。",animationSpeed:1,scale:.36,roaming:false,quiet:false,focusMinutes:25,waterMinutes:0,autoExpressions:true,customActions:[{label:"跟你打招呼",animation:"waving",message:"嗨，今天也一起慢慢来。"},{label:"小小庆祝",animation:"jumping",message:"好耶，完成了一件小事！"},{label:"陪我专注",animation:"running",message:"我陪你专注一会儿。"}]};
const $=id=>document.getElementById(id), canvas=$("petCanvas"), ctx=canvas.getContext("2d");
let profile=JSON.parse(JSON.stringify(DEFAULT)), atlas=new Image(), state=STATES[0], elapsed=0, lastTick=performance.now(), paused=false, theme="paper", gaze=null, customSkin=false;
function notice(message,error=false){$("notice").textContent=message;$("notice").classList.toggle("error",error);}
function bounded(value,min,max,name){if(typeof value!=="number"||!Number.isFinite(value)||value<min||value>max)throw new Error(name+"超出范围 "+min+"–"+max);return value;}
function validateProfile(value){
 if(!value||typeof value!=="object"||Array.isArray(value)||value.schemaVersion!==1)throw new Error("需要 schemaVersion 为 1 的设置 JSON。");
 const allowed=new Set(Object.keys(DEFAULT));for(const key of Object.keys(value))if(!allowed.has(key))throw new Error("不支持的配置项："+key+"。不允许脚本或外部文件路径。");
 const p=JSON.parse(JSON.stringify(DEFAULT));
 for(const [key,max] of [["name",24],["greeting",160]]){if(typeof value[key]!=="string"||value[key].length>max||(key==="name"&&!value[key].trim()))throw new Error(key+"格式不正确。");p[key]=value[key].trim();}
 p.animationSpeed=bounded(value.animationSpeed,.5,2,"动作速度");p.scale=bounded(value.scale,.28,1,"大小");p.focusMinutes=bounded(value.focusMinutes,1,180,"专注时长");p.waterMinutes=bounded(value.waterMinutes,0,240,"喝水间隔");
 if(p.waterMinutes>0&&p.waterMinutes<5)throw new Error("喝水间隔须关闭或至少 5 分钟。");if(!Number.isInteger(p.focusMinutes)||!Number.isInteger(p.waterMinutes))throw new Error("分钟必须为整数。");
 for(const key of ["roaming","quiet","autoExpressions"]){if(typeof value[key]!=="boolean")throw new Error(key+"必须为 true 或 false。");p[key]=value[key];}
 if(!Array.isArray(value.customActions)||value.customActions.length>12)throw new Error("最多允许 12 个自定义动作。");
 p.customActions=value.customActions.map(a=>{if(!a||typeof a!=="object"||typeof a.label!=="string"||!a.label.trim()||a.label.length>20||typeof a.message!=="string"||a.message.length>160)throw new Error("自定义动作名称或台词不正确。");for(const key of Object.keys(a))if(!["label","animation","message"].includes(key))throw new Error("自定义动作只支持名称、动画和台词。");const id=a.animation==="working"?"running":a.animation;if(!STATES.some(s=>s.id===id))throw new Error("自定义动作只允许九种内置动画。");return {label:a.label.trim(),animation:id,message:a.message};});
 return p;
}
function frameAt(s,t){let offset=((t%s.ms.reduce((a,b)=>a+b,0))+s.ms.reduce((a,b)=>a+b,0))%s.ms.reduce((a,b)=>a+b,0);for(let i=0;i<s.ms.length;i++){if(offset<s.ms[i])return i;offset-=s.ms[i];}return 0;}
function setState(id,message){state=STATES.find(s=>s.id===id)||STATES[0];elapsed=0;gaze=null;document.querySelectorAll(".animation-button").forEach(b=>b.classList.toggle("active",b.dataset.state===state.id));$("speech").textContent=message===undefined?profile.greeting:message;}
function render(){
 const width=canvas.clientWidth,height=canvas.clientHeight,dpr=Math.min(2,window.devicePixelRatio||1);if(canvas.width!==Math.round(width*dpr)||canvas.height!==Math.round(height*dpr)){canvas.width=Math.round(width*dpr);canvas.height=Math.round(height*dpr);}ctx.setTransform(dpr,0,0,dpr,0,0);
 ctx.clearRect(0,0,width,height);if(!atlas.complete||!atlas.naturalWidth)return;
 const petHeight=Math.min(height*.68,360),petWidth=petHeight*192/208;
 let row=state.row,col=frameAt(state,elapsed),x=width/2-petWidth/2,y=height-petHeight-54;
 if(state.id.startsWith("running-")&&$("movement").checked){const span=Math.max(0,width-petWidth-40);const progress=(elapsed/2600)%2;const fraction=progress<1?progress:2-progress;const right=state.id==="running-right";x=20+(right?fraction:1-fraction)*span;const facingRight=right?(progress<1):(progress>=1);row=facingRight?1:2;}
 if(state.id==="idle"&&gaze!==null){row=9+Math.floor(gaze/8);col=gaze%8;}
 ctx.imageSmoothingEnabled=true;ctx.imageSmoothingQuality="high";ctx.drawImage(atlas,col*192,row*208,192,208,x,y,petWidth,petHeight);
 $("frameInfo").textContent=(gaze!==null&&state.id==="idle"?"LOOK "+(gaze*22.5).toFixed(1)+"°":state.id.toUpperCase())+" · "+(col+1)+"/"+(row>=9?8:STATES.find(s=>s.row===row).ms.length);
}
function loop(now){const delta=Math.min(100,Math.max(0,now-lastTick));lastTick=now;if(!paused)elapsed+=delta*profile.animationSpeed;render();requestAnimationFrame(loop);}
function updateControls(){
 for(const key of ["name","greeting","scale","focusMinutes","waterMinutes"]){const el=$(key);if(key==="waterMinutes"&&!Array.from(el.options).some(o=>o.value===String(profile[key]))){const option=document.createElement("option");option.value=String(profile[key]);option.textContent="每 "+profile[key]+" 分钟";el.appendChild(option);}el.value=profile[key];}
 $("speed").value=profile.animationSpeed;$("speedValue").textContent=profile.animationSpeed.toFixed(2)+"×";
 for(const key of ["roaming","quiet","autoExpressions"])$(key).checked=profile[key];
 $("previewName").textContent=profile.name;$("speech").textContent=profile.greeting;renderCustomActions();
}
function renderCustomActions(){
 $("customActions").replaceChildren();
 profile.customActions.forEach((a,index)=>{
  const block=document.createElement("div");block.className="custom-action";const row=document.createElement("div");row.className="first-row";
  const title=document.createElement("input");title.value=a.label;title.maxLength=20;title.setAttribute("aria-label","动作 "+(index+1)+" 名称");title.addEventListener("input",()=>a.label=title.value);
  const select=document.createElement("select");select.setAttribute("aria-label","动作 "+(index+1)+" 动画");STATES.forEach(s=>{const o=document.createElement("option");o.value=s.id;o.textContent=s.label;select.appendChild(o);});select.value=a.animation;select.addEventListener("change",()=>{a.animation=select.value;setState(a.animation,a.message);});
  const remove=document.createElement("button");remove.className="remove-action";remove.textContent="×";remove.setAttribute("aria-label","删除动作 "+(index+1));remove.addEventListener("click",()=>{profile.customActions.splice(index,1);renderCustomActions();});row.append(title,select,remove);
  const message=document.createElement("input");message.value=a.message;message.maxLength=160;message.placeholder="点这个动作时说的话";message.setAttribute("aria-label","动作 "+(index+1)+" 台词");message.addEventListener("input",()=>a.message=message.value);const play=document.createElement("button");play.className="text-button";play.textContent="试一下这个动作";play.addEventListener("click",()=>setState(a.animation,a.message));block.append(row,message,play);$("customActions").appendChild(block);
 });$("addAction").disabled=profile.customActions.length>=12;
}
function download(blob,name){const url=URL.createObjectURL(blob),link=document.createElement("a");link.href=url;link.download=name;link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
function fileData(file){return new Promise((resolve,reject)=>{const reader=new FileReader();reader.onload=()=>resolve(reader.result);reader.onerror=()=>reject(new Error("文件读取失败。"));reader.readAsDataURL(file);});}
async function setAtlas(uri,isCustom=false){
 const next=new Image();next.src=uri;await next.decode();if(next.naturalWidth!==1536||next.naturalHeight!==2288)throw new Error("图集必须为 1536 × 2288 像素，8 列 11 行。");
 const probe=document.createElement("canvas");probe.width=1536;probe.height=2288;const pc=probe.getContext("2d",{willReadFrequently:true});pc.drawImage(next,0,0);const pixels=pc.getImageData(0,0,1536,2288).data;
 for(const at of [0,1535,1536*2287,1536*2288-1])if(pixels[at*4+3]!==0)throw new Error("四个角需要完全透明，请导入真实透明 PNG。");
 let visible=0,transparent=0;for(let i=3;i<pixels.length;i+=4){if(pixels[i]>16)visible++;if(pixels[i]===0)transparent++;}if(visible<100||transparent<100)throw new Error("图集需要可见角色和真实透明背景。");
 for(let row=0;row<11;row++)for(let col=0;col<8;col++){const used=row>=9||col<STATES[row].ms.length;let occupied=false;for(let y=row*208;y<(row+1)*208&&!occupied;y++)for(let x=col*192;x<(col+1)*192;x++)if(pixels[(y*1536+x)*4+3]>16){occupied=true;break;}if(used&&!occupied)throw new Error("图集中有空白动作单元，请补齐九类动画和十六个视线方向。");if(!used&&!(row===0&&col===6)&&occupied)throw new Error("未使用的图格必须透明，请使用原生 v2 布局。");}
 atlas=next;customSkin=isCustom;setState("idle");notice(isCustom?"新皮肤已在本页预览。请在桌面版中另行导入同一张 PNG；设置 JSON 不携带图像。":"已恢复默认小黑皮肤。");
}
STATES.forEach(s=>{const b=document.createElement("button");b.className="animation-button"+(s.id==="idle"?" active":"");b.textContent=s.label;b.dataset.state=s.id;b.addEventListener("click",()=>setState(s.id));$("animationButtons").appendChild(b);});
$("pause").addEventListener("click",()=>{paused=!paused;$("pause").textContent=paused?"继续播放":"暂停";});
$("name").addEventListener("input",()=>{profile.name=$("name").value;$("previewName").textContent=profile.name;});$("greeting").addEventListener("input",()=>{profile.greeting=$("greeting").value;$("speech").textContent=profile.greeting;});
$("speed").addEventListener("input",()=>{profile.animationSpeed=Number($("speed").value);$("speedValue").textContent=profile.animationSpeed.toFixed(2)+"×";});
for(const key of ["scale","focusMinutes","waterMinutes"])$(key).addEventListener("change",()=>{profile[key]=Number($(key).value);});for(const key of ["roaming","quiet","autoExpressions"])$(key).addEventListener("change",()=>profile[key]=$(key).checked);
$("addAction").addEventListener("click",()=>{if(profile.customActions.length<12){profile.customActions.push({label:"新动作",animation:"waving",message:"喵，你好呀。"});renderCustomActions();}});
document.querySelectorAll(".swatch").forEach(b=>b.addEventListener("click",()=>{theme=b.dataset.theme;$("stage").className="stage"+(theme==="paper"?"":" "+theme);document.querySelectorAll(".swatch").forEach(s=>s.classList.toggle("selected",s===b));}));
canvas.addEventListener("pointermove",e=>{if(!$("follow").checked||state.id!=="idle")return;const r=canvas.getBoundingClientRect(),dx=(e.clientX-r.left)/r.width-.5,dy=(e.clientY-r.top)/r.height-.45;if(Math.hypot(dx,dy)<.08){gaze=null;return;}const angle=(Math.atan2(dx,-dy)+Math.PI*2)%(Math.PI*2);gaze=Math.round(angle/(Math.PI/8))%16;});canvas.addEventListener("pointerleave",()=>gaze=null);$("follow").addEventListener("change",()=>{gaze=null;setState("idle");});
$("atlasFile").addEventListener("change",async e=>{const file=e.target.files[0];if(!file)return;try{if(file.size>20*1024*1024)throw new Error("PNG 最大 20 MiB。");if(!/\.png$/i.test(file.name))throw new Error("桌面皮肤请使用 PNG。");await setAtlas(await fileData(file),true);}catch(error){notice(error.message,true);}finally{e.target.value="";}});
$("defaultAtlas").addEventListener("click",()=>setAtlas(window.XIAOHEI_SPRITE_URI).catch(e=>notice(e.message,true)));
$("profileFile").addEventListener("change",async e=>{const file=e.target.files[0];if(!file)return;try{if(file.size>65536)throw new Error("设置 JSON 最大 64 KiB。");const next=validateProfile(JSON.parse(await file.text()));profile=next;updateControls();setState("idle");notice("设置已导入。图像不会随 JSON 改变。");}catch(error){notice(error.message,true);}finally{e.target.value="";}});
$("exportProfile").addEventListener("click",()=>{try{const clean=validateProfile(profile);download(new Blob([JSON.stringify(clean,null,2)+"\n"],{type:"application/json"}),"pet.profile.json");notice("设置已导出。在桌面版设置页导入 JSON 即可应用。");}catch(error){notice(error.message,true);}});
$("saveImage").addEventListener("click",()=>{
 if(!atlas.complete||!atlas.naturalWidth)return;const out=document.createElement("canvas");out.width=1080;out.height=1440;const p=out.getContext("2d");p.fillStyle=theme==="night"?"#293e3a":theme==="mint"?"#d9e7dc":"#f5f4e9";p.fillRect(0,0,1080,1440);const dark=theme!=="night";p.fillStyle=dark?"#304a36":"#edf2dd";p.font='22px "Microsoft YaHei UI", sans-serif';p.fillText("小黑搭子 / COMPANION STUDIO",82,100);p.font='bold 72px "Microsoft YaHei UI", sans-serif';p.fillText(profile.name.slice(0,10),78,230);p.font='28px "Microsoft YaHei UI", sans-serif';p.fillText(state.label+" · 真实动画帧预览",82,291);let row=state.row,col=frameAt(state,elapsed);if(state.id==="idle"&&gaze!==null){row=9+Math.floor(gaze/8);col=gaze%8;}p.imageSmoothingQuality="high";p.drawImage(atlas,col*192,row*208,192,208,213,420,654,708);p.fillStyle=dark?"#647953":"#bdcfb1";p.font='25px "Microsoft YaHei UI", sans-serif';p.fillText(profile.greeting.slice(0,30),82,1225);p.font='18px "Microsoft YaHei UI", sans-serif';p.fillText(customSkin?"自定义皮肤预览 · 本地生成":"非官方同人作品 · AI 辅助绘制动画",82,1340);p.fillText("程序代码开放许可不包含角色素材权利",82,1373);out.toBlob(blob=>{if(blob){download(blob,"小黑搭子-预览.png");notice("已保存 1080 × 1440 预览图，使用当前真实动画帧。");}},"image/png");
});
window.PetStudio={states:STATES,getProfile:()=>JSON.parse(JSON.stringify(profile)),validateProfile,setState,frameAt,setAtlas,getState:()=>state.id};
updateControls();setAtlas(window.XIAOHEI_SPRITE_URI).then(()=>notice("编辑器已就绪；设置不会自动保存，请导出后保留。")).catch(e=>notice(e.message,true));requestAnimationFrame(loop);
