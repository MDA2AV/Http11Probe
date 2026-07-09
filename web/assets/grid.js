/* Http11Probe results matrix. Reads window.PROBE_DATA + window.SLUGMAP. */
(function(){
  const DATA = window.PROBE_DATA;
  const SLUG = window.SLUGMAP || {};
  const RFCMAP = window.RFCMAP || {};   // testId -> "RFC <n> §<s>" from glossary pages (fallback when probe data has none)
  const root = document.getElementById("results");
  if(!DATA || !DATA.servers){ root.innerHTML='<p class="prose">No probe data loaded. Run the pipeline to generate <code>data.js</code>.</p>'; return; }
  const CFG = window.GRID_CONFIG || {};   // {cats,levels,showCatChips,showEntropy,sort}

  // ----- server tiers (editable classification) -----
  const TIERS = {
    Infrastructure:["Nginx","Apache","HAProxy","Envoy","Caddy","Traefik","Pingora","Lighttpd","H2O"],
    Flagship:["Kestrel","Spring Boot","Tomcat","Jetty","Quarkus","Node","Express","Deno","Bun","Uvicorn","Gunicorn","Flask","Puma","Gin","Actix","Hyper"],
    Emerging:["FastHTTP","Ntex","Sisk","ServiceStack","GenHTTP","Workerman","Trillium"],
    Experimental:["Swerver","Horse","Glyph11","Effinitive","SimpleW","NetCoreServer","EmbedIO","FastEndpoints","FastPySGI","GenHTTP 11","PHP"],
  };
  const TIER_ORDER=["Flagship","Infrastructure","Emerging","Experimental","Other"];
  const tierOf={}; for(const t in TIERS) TIERS[t].forEach(n=>tierOf[n]=t);

  const GLYPH={Pass:"●",Warn:"▲",Fail:"✕",Error:"!",Skip:"·",NA:"·"};

  // ----- model -----
  const servers = DATA.servers.map(s=>{
    const scored=s.results.filter(r=>r.scored);
    const pass=scored.filter(r=>r.verdict==="Pass").length;
    const warn=scored.filter(r=>r.verdict==="Warn").length;
    return { name:s.name, lang:s.language||"", tier:tierOf[s.name]||"Other",
      score:pass+warn, pass, warn, fail:scored.length-pass-warn, scored:scored.length,
      byId:Object.fromEntries(s.results.map(r=>[r.id,r])) };
  });
  const srvByName=Object.fromEntries(servers.map(s=>[s.name,s]));
  // canonical test list + metadata from the first server
  const base=DATA.servers[0].results;
  let tests=base.map(r=>({ id:r.id, cat:r.category, lvl:r.rfcLevel||"Must", scored:r.scored!==false,
    exp:r.expected, desc:r.description, rfc:r.rfc||r.rfcReference||null, url:SLUG[r.id]||null }));
  if(CFG.cats) tests=tests.filter(t=>CFG.cats.includes(t.cat));
  if(CFG.levels) tests=tests.filter(t=>CFG.levels.includes(t.lvl));
  const CATS=[...new Set(tests.map(t=>t.cat))];

  const defaultServers = CFG.defaultTiers ? servers.filter(s=>CFG.defaultTiers.includes(s.tier)) : servers;
  const state={ sel:new Set(defaultServers.map(s=>s.name)), cats:new Set(CATS), divOnly:false, scoredOnly:false };
  let INSIGHT=null;   // matrix-study metrics for the Entropy page (recomputed each render)

  // ----- helpers -----
  const el=(t,c,h)=>{const e=document.createElement(t);if(c)e.className=c;if(h!=null)e.innerHTML=h;return e;};
  const esc=s=>String(s==null?"":s).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;");
  function statusText(r){
    if(!r) return "—";
    if(r.statusCode) return String(r.statusCode);
    switch(r.connectionState){case "ClosedByServer":return "close";case "TimedOut":return "t/o";case "Error":return "err";}
    return "—";
  }
  // grid display name: replace the category/RFC id prefix with "<rfc>-<section>-"
  function displayName(t){
    const core=t.id.replace(/^(RFC\d+-[\d.]+-|COMP-|SMUG-|MAL-|NORM-|COOK-|CAP-|WS-)/i,"");
    const m=(t.rfc||RFCMAP[t.id]||"").match(/RFC\s*(\d+\w*)\s*§?\s*([\d.]+)/i);
    return m?`${m[1]}-${m[2]}-${core}`:core;
  }
  function selectedServers(){
    return servers.filter(s=>state.sel.has(s.name)).sort((a,b)=>b.score-a.score);
  }
  function verdictCounts(t,srvs){
    const c={Pass:0,Warn:0,Fail:0,Other:0};
    srvs.forEach(s=>{const v=s.byId[t.id]?.verdict; if(c[v]!=null)c[v]++; else c.Other++;});
    return c;
  }
  function disagreement(t,srvs){
    const c=verdictCounts(t,srvs); const tot=srvs.length||1;
    const top=Math.max(c.Pass,c.Warn,c.Fail,c.Other);
    return 1-top/tot;
  }
  function entropy(t,srvs){   // Shannon entropy (bits) of the verdict distribution
    const c={}; let n=0;
    srvs.forEach(s=>{const r=s.byId[t.id]; const v=r&&r.verdict; if(v){c[v]=(c[v]||0)+1;n++;}});
    if(!n) return 0;
    let h=0; for(const k in c){const p=c[k]/n; h-=p*Math.log2(p);}
    return h;
  }
  const ENT_MAX=Math.log2(3);   // max for {Pass,Warn,Fail} — bar normalisation
  // ---- global structure metrics (Entropy insight panel) ----
  function raschResiduals(V){               // additive-logit fit logit p = theta_col - beta_row; residual = obs - pred
    const m=V.length, n=V[0].length;
    const rowsum=V.map(r=>r.reduce((a,b)=>a+b,0));
    const colsum=new Array(n).fill(0); for(let i=0;i<m;i++)for(let j=0;j<n;j++)colsum[j]+=V[i][j];
    const theta=new Array(n).fill(0), beta=new Array(m).fill(0);
    const lg=x=>1/(1+Math.exp(-Math.max(-30,Math.min(30,x))));
    for(let it=0;it<200;it++){
      for(let i=0;i<m;i++){ let pr=0,dd=0; for(let j=0;j<n;j++){const p=lg(theta[j]-beta[i]); pr+=p; dd+=p*(1-p);}
        beta[i]=Math.max(-8,Math.min(8, beta[i]+(pr-rowsum[i])/Math.max(dd,1e-6))); }
      for(let j=0;j<n;j++){ let pc=0,dd=0; for(let i=0;i<m;i++){const p=lg(theta[j]-beta[i]); pc+=p; dd+=p*(1-p);}
        theta[j]=Math.max(-8,Math.min(8, theta[j]-(pc-colsum[j])/Math.max(dd,1e-6))); }
    }
    const gm=rowsum.reduce((a,b)=>a+b,0)/(m*n); let ssTot=0,ssRes=0;
    const resid=V.map((r,i)=>r.map((v,j)=>{const e=v-lg(theta[j]-beta[i]); ssTot+=(v-gm)**2; ssRes+=e*e; return e;}));
    return {resid, explained:ssTot>0?1-ssRes/ssTot:0};
  }
  function computeInsight(vts,srvs){
    const m=vts.length,n=srvs.length,enc={Pass:1,Warn:0.5,Fail:0};
    const V=vts.map(t=>srvs.map(s=>{const r=s.byId[t.id]; return r?(enc[r.verdict]??0):0;}));
    const density=V.reduce((a,r)=>a+r.reduce((x,y)=>x+y,0),0)/(m*n);
    const explained=raschResiduals(V).explained;
    // binary compliance matrix (pass=1 else 0) for the structure metrics
    const B=vts.map(t=>srvs.map(s=>((s.byId[t.id]||{}).verdict==="Pass")?1:0));
    // systematic vs idiosyncratic: share of all fails that land on tests >=3 servers fail
    let totalFails=0, systematic=0;
    for(let i=0;i<m;i++){ let fc=0; for(let j=0;j<n;j++) if(!B[i][j]) fc++;
      totalFails+=fc; if(fc>=3) systematic+=fc; }
    const systematicPct = totalFails>0 ? systematic/totalFails : 1;
    // behavioural families: complete-linkage clusters (EVERY pair in a family agrees >=85%)
    const A=Array.from({length:n},()=>new Array(n).fill(1));
    for(let i=0;i<n;i++)for(let j=i+1;j<n;j++){let s=0;for(let k=0;k<m;k++)if(B[k][i]===B[k][j])s++; A[i][j]=A[j][i]=s/m;}
    let clusters=srvs.map((_,i)=>[i]);
    const cdist=(a,b)=>{let mn=1;for(const x of a)for(const y of b)if(A[x][y]<mn)mn=A[x][y];return mn;};
    for(;;){ let best=-1,bi=-1,bj=-1;
      for(let a=0;a<clusters.length;a++)for(let b=a+1;b<clusters.length;b++){const d=cdist(clusters[a],clusters[b]); if(d>=0.85&&d>best){best=d;bi=a;bj=b;}}
      if(bi<0)break; clusters[bi]=clusters[bi].concat(clusters[bj]); clusters.splice(bj,1);
    }
    clusters.sort((a,b)=>b.length-a.length);
    const famGroups=clusters.filter(c=>c.length>=2).map(c=>c.map(i=>srvs[i].name));
    const loners=clusters.filter(c=>c.length===1).map(c=>srvs[c[0]].name);
    const famOf={}; famGroups.forEach((g,idx)=>g.forEach(nm=>famOf[nm]=idx+1));
    // per-test stats for the test-name hover
    const byId={};
    for(let i=0;i<m;i++){ let passN=0; for(let j=0;j<n;j++) if(B[i][j]) passN++;
      byId[vts[i].id]={passN,H:entropy(vts[i],srvs)}; }
    return {density,explained,systematicPct,families:famGroups.length,famGroups,loners,famOf,n,byId};
  }
  function renderInsight(INS){
    const box=document.getElementById("insight"); if(!box) return;
    if(!INS){ box.innerHTML=""; return; }
    const tiles=[
      ["Density",(INS.density*100).toFixed(0)+"%","MUST verdicts that pass (warn = ½)"],
      ["Strictness explains",Math.round(INS.explained*100)+"%","captured by a simple difficulty × strictness gradient"],
      ["Systematic failures",(INS.systematicPct*100).toFixed(0)+"%","of fails land on tests ≥3 servers fail — shared, not scattered"],
      ["Behavioural families",String(INS.families),"groups agreeing ≥85% among the "+INS.n+" shown servers"],
    ];
    box.innerHTML=tiles.map(([k,v,d])=>`<div class="itile"><div class="iv">${v}</div><div class="ik">${k}</div><div class="idesc">${d}</div></div>`).join("");
  }
  function renderFamilies(INS){
    const box=document.getElementById("families"); if(!box) return;
    if(!INS||!INS.famGroups){ box.innerHTML=""; return; }
    let html=`<div class="fam-title">Behavioural families <span>· every pair in a family gives the same verdict ≥85% of the time</span></div><div class="fam-grid">`;
    INS.famGroups.forEach((g,i)=>{
      html+=`<div class="fam-card"><div class="fam-h"><span class="fam-tag">F${i+1}</span> ${g.length} servers</div>`+
        `<div class="fam-chips">${g.map(nm=>`<span class="fam-chip">${esc(nm)}</span>`).join("")}</div></div>`;
    });
    if(INS.loners.length) html+=`<div class="fam-card loners"><div class="fam-h">Distinct · ${INS.loners.length}</div>`+
      `<div class="fam-chips">${INS.loners.map(nm=>`<span class="fam-chip">${esc(nm)}</span>`).join("")}</div></div>`;
    box.innerHTML=html+`</div>`;
  }
  function visibleTests(srvs){
    let ts=tests.filter(t=>state.cats.has(t.cat)&&(!state.scoredOnly||t.scored));
    ts=ts.map(t=>({t,d:disagreement(t,srvs),h:entropy(t,srvs)}));
    if(state.divOnly) ts=ts.filter(x=>x.d>0);
    const key=CFG.sort==="entropy"?(x=>x.h):(x=>x.d);
    ts.sort((a,b)=>key(b)-key(a)||a.t.id.localeCompare(b.t.id));
    ts.forEach(x=>x.t._h=x.h);
    return ts.map(x=>x.t);
  }

  // ----- framework selector -----
  function renderSelector(){
    const menu=document.getElementById("fw-menu");
    const quick=el("div","fw-quick");
    const mk=(label,fn)=>{const b=el("button",null,label);b.onclick=(e)=>{e.preventDefault();fn();syncSelector();render();};return b;};
    quick.append(
      mk("All",()=>servers.forEach(s=>state.sel.add(s.name))),
      mk("None",()=>state.sel.clear()),
    );
    TIER_ORDER.forEach(tier=>{
      const inTier=servers.filter(s=>s.tier===tier);
      if(!inTier.length)return;
      quick.append(mk("Only "+tier,()=>{state.sel.clear();inTier.forEach(s=>state.sel.add(s.name));}));
    });
    menu.appendChild(quick);
    TIER_ORDER.forEach(tier=>{
      const inTier=servers.filter(s=>s.tier===tier).sort((a,b)=>b.score-a.score);
      if(!inTier.length)return;
      const sec=el("div","fw-tier");
      sec.appendChild(el("h4",null,`${tier} <span style="opacity:.6">${inTier.length}</span>`));
      const opts=el("div","opts");
      inTier.forEach(s=>{
        const lab=el("label","fw-opt");
        const cb=el("input");cb.type="checkbox";cb.checked=state.sel.has(s.name);cb.dataset.srv=s.name;
        cb.onchange=()=>{cb.checked?state.sel.add(s.name):state.sel.delete(s.name);updateSelCount();render();};
        lab.append(cb,document.createTextNode(s.name),el("span","lg",s.lang));
        opts.appendChild(lab);
      });
      sec.appendChild(opts);menu.appendChild(sec);
    });
    updateSelCount();
    // dropdown open/close
    const trigger=document.getElementById("fw-trigger");
    const open=()=>{menu.hidden=false;trigger.setAttribute("aria-expanded","true");};
    const close=()=>{menu.hidden=true;trigger.setAttribute("aria-expanded","false");};
    trigger.onclick=(e)=>{e.stopPropagation();menu.hidden?open():close();};
    menu.addEventListener("click",e=>e.stopPropagation());
    document.addEventListener("click",close);
    document.addEventListener("keydown",e=>{if(e.key==="Escape")close();});
  }
  function syncSelector(){
    document.querySelectorAll('#fw-menu input[type=checkbox]').forEach(cb=>{cb.checked=state.sel.has(cb.dataset.srv);});
    updateSelCount();
  }
  function updateSelCount(){
    document.getElementById("selcount").textContent=`${state.sel.size} / ${servers.length}`;
  }

  // ----- category chips -----
  function renderChips(){
    const bar=document.getElementById("chips");
    if(CFG.showCatChips!==false){
      const refresh=()=>[...bar.querySelectorAll(".chip[data-cat]")].forEach(x=>x.setAttribute("aria-pressed",state.cats.has(x.dataset.cat)));
      const allBtn=el("button","chip","All");
      allBtn.onclick=()=>{state.cats=new Set(CATS);refresh();render();};
      bar.appendChild(allBtn);
      const noneBtn=el("button","chip","None");
      noneBtn.onclick=()=>{state.cats.clear();refresh();render();};
      bar.appendChild(noneBtn);
      CATS.forEach(cat=>{
        const n=tests.filter(t=>t.cat===cat).length;
        const b=el("button","chip",`${cat}<span class="n">${n}</span>`);
        b.dataset.cat=cat;
        b.setAttribute("aria-pressed",state.cats.has(cat));
        b.onclick=()=>{state.cats.has(cat)?state.cats.delete(cat):state.cats.add(cat);
          b.setAttribute("aria-pressed",state.cats.has(cat));render();};
        bar.appendChild(b);
      });
    }
    const tgl=el("button","chip","divergent only");tgl.dataset.tgl="1";tgl.style.marginLeft="4px";
    tgl.setAttribute("aria-pressed",state.divOnly);
    tgl.onclick=()=>{state.divOnly=!state.divOnly;tgl.setAttribute("aria-pressed",state.divOnly);render();};
    bar.appendChild(tgl);
    const stgl=el("button","chip","scored only");stgl.dataset.tgl="1";
    stgl.setAttribute("aria-pressed",state.scoredOnly);
    stgl.onclick=()=>{state.scoredOnly=!state.scoredOnly;stgl.setAttribute("aria-pressed",state.scoredOnly);render();};
    bar.appendChild(stgl);
    const lg=el("div","legend");
    lg.innerHTML=`<span><i class="swatch sw-pass">${GLYPH.Pass}</i>pass</span>
      <span><i class="swatch sw-warn">${GLYPH.Warn}</i>warn</span>
      <span><i class="swatch sw-fail">${GLYPH.Fail}</i>fail</span>`;
    bar.appendChild(lg);
  }

  // ----- matrix -----
  function render(){
    const srvs=selectedServers();
    const head=document.getElementById("mhead"), body=document.getElementById("mbody");
    head.innerHTML="";body.innerHTML="";
    if(!srvs.length){ document.getElementById("rowcount").textContent="No frameworks selected."; return; }
    const vts=visibleTests(srvs);
    INSIGHT=(CFG.showEntropy && vts.length>=3 && srvs.length>=3)?computeInsight(vts,srvs):null;
    renderInsight(INSIGHT); renderFamilies(INSIGHT);
    const tr=el("tr");
    tr.appendChild(el("th","corner",`test · ${srvs.length} shown →`));
    if(CFG.showEntropy) tr.appendChild(el("th","ent-h","entropy<br>bits"));
    srvs.forEach(s=>{const th=el("th","srv");th.title=`${s.name} — ${s.score}/${s.scored} (${s.tier})`;
      const fam=INSIGHT&&INSIGHT.famOf?INSIGHT.famOf[s.name]:null;
      th.innerHTML=`<div class="rot">${s.name}</div><div class="sc">${s.score}</div>`+(fam?`<div class="fam-badge">F${fam}</div>`:"");
      tr.appendChild(th);});
    head.appendChild(tr);
    const frag=document.createDocumentFragment();
    vts.forEach(t=>{
      const row=el("tr",t.scored?null:"unscored");
      const rid=el("td","rid");
      rid.dataset.t=t.id;
      const nm=esc(displayName(t));
      const idHtml=t.url?`<a href="${t.url}" title="${esc(t.id)}">${nm}</a>`:`<span title="${esc(t.id)}">${nm}</span>`;
      rid.innerHTML=`${idHtml}<span class="lv${t.lvl==="Must"?" must":""}">${t.lvl==="Must"?"MUST":t.lvl}</span>`;
      row.appendChild(rid);
      if(CFG.showEntropy){
        const ent=el("td","ent");
        const pct=Math.min(100,Math.round((t._h||0)/ENT_MAX*100));
        ent.innerHTML=`<span class="ent-v">${(t._h||0).toFixed(2)}</span><span class="ent-bar"><i style="width:${pct}%"></i></span>`;
        row.appendChild(ent);
      }
      srvs.forEach(s=>{
        const r=s.byId[t.id]; const v=r?r.verdict:"NA";
        const td=el("td","cell "+v);
        const code=esc(statusText(r));
        td.innerHTML=t.url?`<a href="${t.url}" tabindex="-1">${code}</a>`:`<span>${code}</span>`;
        td.dataset.s=s.name;td.dataset.t=t.id;td.dataset.v=v;
        row.appendChild(td);
      });
      frag.appendChild(row);
    });
    body.appendChild(frag);
    document.getElementById("rowcount").textContent=
      `${vts.length} tests × ${srvs.length} frameworks = ${vts.length*srvs.length} verdicts`+
      (state.scoredOnly?" · scored only":"")+(state.divOnly?" · divergent only":"")+
      (state.cats.size<CATS.length?` · ${state.cats.size} categor${state.cats.size===1?"y":"ies"}`:"");
  }

  // ----- tooltip + column sort -----
  function wireInteractions(){
    const tip=document.getElementById("tip");const tById=Object.fromEntries(tests.map(t=>[t.id,t]));
    const mx=document.getElementById("matrix");
    const trunc=(x,n)=>{x=x||"";return x.length>n?x.slice(0,n)+" …[truncated]":x;};
    mx.addEventListener("mouseover",e=>{
      const rid=e.target.closest("td.rid");
      if(rid){
        const t=tById[rid.dataset.t]; if(!t) return;
        const st=INSIGHT&&INSIGHT.byId[t.id];
        tip.innerHTML=
          `<div class="tip-h"><b>${esc(t.id)}</b></div>`+
          `<div class="tip-sub">${esc(t.cat)} · ${esc(t.lvl==="Must"?"MUST":t.lvl)}${t.rfc?" · "+esc(t.rfc):""} · expected ${esc(t.exp||"?")}</div>`+
          `<div class="tip-desc">${esc(t.desc||"No description available.")}</div>`+
          (st?`<div class="tip-stat">entropy <b>${st.H.toFixed(2)}</b> bits · <b>${st.passN}/${INSIGHT.n}</b> pass</div>`:"")+
          (t.url?`<div class="tip-foot">Click the name to open the full test page →</div>`:"");
        positionTip(rid,tip);
        document.querySelectorAll("td.cell.hl").forEach(x=>x.classList.remove("hl"));
        return;
      }
      const c=e.target.closest("td.cell");if(!c)return;
      const t=tById[c.dataset.t], s=srvByName[c.dataset.s], r=s&&s.byId[c.dataset.t];
      const req=(r&&r.rawRequest)?trunc(r.rawRequest,700):"(request unavailable)";
      const res=(r&&r.rawResponse)?trunc(r.rawResponse,700)
        :((r&&r.connectionState==="ClosedByServer")?"(connection closed by server — no response)":"(no response captured)");
      tip.innerHTML=
        `<div class="tip-h"><b>${esc(c.dataset.s)}</b> · <span class="v-${c.dataset.v}">${(c.dataset.v||"n/a").toUpperCase()}</span> → ${esc(statusText(r))}</div>`+
        `<div class="tip-sub">${esc(t.id)} · ${esc(t.cat)} · ${esc(t.lvl)} · expected ${esc(t.exp||"")}</div>`+
        `<span class="lbl">request sent</span><pre>${esc(req)}</pre>`+
        `<span class="lbl">response</span><pre>${esc(res)}</pre>`;
      positionTip(c,tip);
      document.querySelectorAll("td.cell.hl").forEach(x=>x.classList.remove("hl"));c.classList.add("hl");
    });
    mx.addEventListener("mouseleave",()=>{tip.style.opacity="0";document.querySelectorAll("td.cell.hl").forEach(x=>x.classList.remove("hl"));});
  }
  function positionTip(cell,tip){
    tip.style.opacity="1";
    const rect=cell.getBoundingClientRect(), gap=10;
    const w=tip.offsetWidth,h=tip.offsetHeight;
    let x=rect.right+gap; if(x+w>innerWidth) x=rect.left-w-gap; if(x<6) x=6;
    let y=rect.top; if(y+h>innerHeight) y=innerHeight-h-8; if(y<6) y=6;
    tip.style.left=x+"px";tip.style.top=y+"px";
  }

  // provenance strip
  const c=DATA.commit||{};
  document.getElementById("provstrip").innerHTML=
    `<div><span class="k">servers</span>${servers.length}</div>`+
    `<div><span class="k">tests</span>${tests.length}</div>`+
    (c.id?`<div><span class="k">commit</span>${c.id.slice(0,7)}</div>`:"")+
    (c.timestamp?`<div><span class="k">run</span>${c.timestamp.slice(0,10)}</div>`:"");

  document.getElementById("matrix").classList.toggle("we",!!CFG.showEntropy);
  renderSelector();renderChips();wireInteractions();render();
})();
