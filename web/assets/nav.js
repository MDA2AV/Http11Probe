/* Http11Probe site chrome: theme, mobile nav, glossary search. */
(function(){
  // ---- theme (persisted) ----
  try{ const t=localStorage.getItem("h11-theme"); if(t) document.documentElement.setAttribute("data-theme",t); }catch(e){}
  const themeBtn=document.getElementById("theme");
  if(themeBtn) themeBtn.onclick=()=>{
    const cur=document.documentElement.getAttribute("data-theme")
      ||(matchMedia("(prefers-color-scheme:dark)").matches?"dark":"light");
    const next=cur==="dark"?"light":"dark";
    document.documentElement.setAttribute("data-theme",next);
    try{localStorage.setItem("h11-theme",next);}catch(e){}
  };

  // ---- mobile nav ----
  const mt=document.getElementById("menu-toggle");
  if(mt) mt.onclick=()=>document.body.classList.toggle("nav-open");
  const scrim=document.querySelector(".scrim");
  if(scrim) scrim.onclick=()=>document.body.classList.remove("nav-open");

  // ---- search ----
  const IDX=window.SEARCH_INDEX||[];
  const input=document.getElementById("search-input");
  const box=document.getElementById("search-results");
  if(!input||!box) return;
  let sel=-1, cur=[];
  const norm=s=>s.toLowerCase();
  function query(q){
    q=norm(q).trim(); if(!q) return [];
    const terms=q.split(/\s+/);
    return IDX.map(it=>{
      const hay=norm(it.title+" "+(it.id||"")+" "+(it.cat||""));
      let score=0;
      for(const t of terms){ const i=hay.indexOf(t); if(i<0) return null; score+= i===0?3:(hay.includes(" "+t)?2:1); }
      if(it.id && norm(it.id).includes(q)) score+=5;
      return {it,score};
    }).filter(Boolean).sort((a,b)=>b.score-a.score).slice(0,12).map(x=>x.it);
  }
  function draw(){
    if(!cur.length){ box.innerHTML='<div class="empty">No matching tests or pages.</div>'; box.classList.add("on"); return; }
    box.innerHTML=cur.map((it,i)=>
      `<a href="${it.url}" class="${i===sel?'sel':''}">${it.title}`+
      (it.id?` <span class="cat">${it.id}</span>`:it.cat?` <span class="cat">${it.cat}</span>`:"")+`</a>`).join("");
    box.classList.add("on");
  }
  input.addEventListener("input",()=>{ cur=query(input.value); sel=-1; input.value.trim()?draw():box.classList.remove("on"); });
  input.addEventListener("keydown",e=>{
    if(!box.classList.contains("on"))return;
    if(e.key==="ArrowDown"){e.preventDefault();sel=Math.min(sel+1,cur.length-1);draw();}
    else if(e.key==="ArrowUp"){e.preventDefault();sel=Math.max(sel-1,0);draw();}
    else if(e.key==="Enter"&&sel>=0&&cur[sel]){location.href=cur[sel].url;}
    else if(e.key==="Escape"){box.classList.remove("on");input.blur();}
  });
  document.addEventListener("click",e=>{ if(!e.target.closest(".search")) box.classList.remove("on"); });
  input.addEventListener("focus",()=>{ if(input.value.trim()){cur=query(input.value);draw();} });
})();
