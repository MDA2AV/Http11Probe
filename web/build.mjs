// Http11Probe static site generator. Reads ../docs/content markdown, emits ./dist.
import { createRequire } from "module";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
const require = createRequire(import.meta.url);
const MarkdownIt = require("markdown-it");

const __dir = path.dirname(fileURLToPath(import.meta.url));
const CONTENT = path.resolve(__dir, "../docs/content");
const DIST = path.resolve(__dir, "dist");
const ASSETS = path.resolve(__dir, "assets");
const GH = "https://github.com/MDA2AV/Http11Probe";

const md = new MarkdownIt({ html: true, linkify: true, breaks: false });

// ---------- helpers ----------
const read = p => fs.readFileSync(p, "utf8");
const exists = p => fs.existsSync(p);
function walk(dir){ const out=[]; if(!exists(dir))return out;
  for(const e of fs.readdirSync(dir,{withFileTypes:true})){
    const p=path.join(dir,e.name);
    if(e.isDirectory()) out.push(...walk(p)); else if(e.name.endsWith(".md")) out.push(p);
  } return out; }
function ensure(p){ fs.mkdirSync(path.dirname(p),{recursive:true}); }
function esc(s){ return s.replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;"); }

function frontmatter(src){
  const m = src.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n?([\s\S]*)$/);
  if(!m) return { fm:{}, body:src };
  const fm={};
  for(const line of m[1].split(/\r?\n/)){
    const mm=line.match(/^([A-Za-z_][\w-]*):\s*(.*)$/);         // top-level scalars only
    if(mm){ let v=mm[2].trim().replace(/^["']|["']$/g,""); fm[mm[1]]=v; }
  }
  return { fm, body:m[2] };
}

// ---------- Hugo shortcode preprocessing ----------
function preprocess(body, urlDir){
  // cards block
  body = body.replace(/\{\{<\s*cards\s*>\}\}([\s\S]*?)\{\{<\s*\/cards\s*>\}\}/g, (_,inner)=>{
    const cards=[...inner.matchAll(/\{\{<\s*card\s+([^>]*?)\s*(?:\/)?>\}\}/g)].map(c=>{
      const a={}; for(const at of c[1].matchAll(/(\w+)="([^"]*)"/g)) a[at[1]]=at[2];
      const href = a.link ? (a.link.startsWith("/")||a.link.startsWith("http") ? a.link : `${urlDir}/${a.link}.html`) : "#";
      return `<a class="card" href="${href}"><div class="card-t">${esc(a.title||a.link||"")}</div>`+
             (a.subtitle?`<div class="card-s">${esc(a.subtitle)}</div>`:"")+`</a>`;
    }).join("");
    return `\n<div class="cards">${cards}</div>\n`;
  });
  // callout
  body = body.replace(/\{\{<\s*callout[^>]*>\}\}([\s\S]*?)\{\{<\s*\/callout\s*>\}\}/g,
    (_,inner)=>`\n<div class="callout">\n\n${inner.trim()}\n\n</div>\n`);
  // relref -> best-effort local link
  body = body.replace(/\{\{<\s*relref\s+"([^"]+)"\s*>\}\}/g, (_,t)=>"/docs/"+t.replace(/\.md$/,"").replace(/^\//,"")+".html");
  // strip remaining shortcodes (hero, icon, etc.)
  body = body.replace(/\{\{[<%]\s*\/?[^}]*?[%>]\}\}/g, "");
  return body;
}

// ---------- content model ----------
// section: Overview | Glossary | Servers | Contributing ; group label within section
const CAT_TITLE = {}; // folder -> label (from _index)
function catLabelFromIndex(folder){
  const idx=path.join(CONTENT,"docs",folder,"_index.md");
  if(exists(idx)){ const {fm}=frontmatter(read(idx)); if(fm.title) return fm.title; }
  return folder.replace(/(^|-)(\w)/g,(_,a,b)=>(a?" ":"")+b.toUpperCase());
}

function buildPages(){
  const pages=[];
  const push=(srcPath, url, extra)=>{
    const {fm,body}=frontmatter(read(srcPath));
    pages.push({ srcPath, url, body, title:fm.title||path.basename(srcPath,".md"),
      weight:parseInt(fm.weight)||999, ...extra });
  };
  // docs/**
  for(const p of walk(path.join(CONTENT,"docs"))){
    const rel=path.relative(path.join(CONTENT,"docs"),p).replace(/\\/g,"/");
    const isIndex=path.basename(p)==="_index.md";
    const parts=rel.split("/");
    let url;
    if(isIndex) url = parts.length===1 ? "/docs/" : `/docs/${parts.slice(0,-1).join("/")}/`;
    else url = `/docs/${rel.replace(/\.md$/,"")}.html`;
    const folder = parts.length>1 ? parts[0] : "";
    let section, group;
    if(folder==="" ){ section="Overview"; group="Introduction"; }
    else if(folder==="http-overview"){ section="Overview"; group="HTTP/1.1 Overview"; }
    else { section="Glossary"; group=catLabelFromIndex(folder); CAT_TITLE[folder]=group; }
    push(p, url, {section, group, folder, isIndex});
  }
  // servers/**
  for(const p of walk(path.join(CONTENT,"servers"))){
    const base=path.basename(p,".md");
    const isIndex=base==="_index";
    push(p, isIndex?"/servers/":`/servers/${base}.html`, {section:"Servers", group:"Servers", isIndex});
  }
  // contributing
  const contrib=[["add-a-test.md","/add-a-test.html","Add a Test"],
    ["add-with-ai-agent.md","/add-with-ai-agent.html","Add with an AI Agent"],
    ["add-a-framework/_index.md","/add-a-framework.html","Add a Framework"]];
  for(const [rel,url,label] of contrib){ const sp=path.join(CONTENT,rel);
    if(exists(sp)) push(sp,url,{section:"Contributing",group:"Contributing",forceTitle:label,isIndex:true}); }
  return pages;
}

// ---------- nav ----------
const SECTION_ORDER=["Overview","Glossary","Servers","Contributing"];
function buildNav(pages, activeUrl){
  const bySection={};
  for(const pg of pages){ (bySection[pg.section] ||= {}); (bySection[pg.section][pg.group] ||= []).push(pg); }
  let html=`<a class="nav-link top ${activeUrl==="/"?"active":""}" href="/">Results matrix</a>`;
  html+=`<a class="nav-link top ${activeUrl==="/entropy.html"?"active":""}" href="/entropy.html">Entropy</a>`;
  for(const section of SECTION_ORDER){
    const groups=bySection[section]; if(!groups) continue;
    const gkeys=Object.keys(groups).sort((a,b)=>gWeight(groups[a])-gWeight(groups[b]));
    const sectionActive=Object.values(groups).flat().some(it=>it.url===activeUrl);
    let inner="";
    if(section==="Glossary"){
      // Glossary keeps a second level: one collapsible group per category
      for(const g of gkeys){
        const items=groups[g].slice().sort((a,b)=>a.weight-b.weight);
        const hasActive=items.some(it=>it.url===activeUrl);
        inner+=`<details class="nav-group"${hasActive?" open":""}><summary>${esc(g)} <span class="count">${items.length}</span><span class="chev">▸</span></summary><div class="nav-items">`;
        for(const it of items) inner+=link(it,activeUrl,"");
        inner+=`</div></details>`;
      }
    } else {
      // Overview / Servers / Contributing: flat links under the section
      for(const g of gkeys) for(const it of groups[g].slice().sort((a,b)=>a.weight-b.weight)) inner+=link(it,activeUrl,"");
    }
    html+=`<details class="nav-section"${sectionActive?" open":""}><summary>${section}<span class="chev">▸</span></summary><div class="nav-sec-body">${inner}</div></details>`;
  }
  return html;
}
function gWeight(items){ const idx=items.find(i=>i.isIndex); return idx?idx.weight:Math.min(...items.map(i=>i.weight)); }
function navTitle(pg){ return pg.forceTitle || (pg.isIndex && pg.section==="Glossary" ? "Overview" : pg.title); }
function link(pg,active,cls){
  const t = pg.isIndex && pg.section!=="Contributing" && pg.group!=="Introduction" ? (pg.section==="Servers"?"All servers":"Overview") : navTitle(pg);
  return `<a class="nav-link ${cls} ${pg.url===active?"active":""}" href="${pg.url}">${esc(t)}</a>`;
}

// ---------- template ----------
function shell({title, navHtml, bodyHtml, breadcrumb, wide, headExtra="", scripts="", contentClass="prose"}){
  return `<!doctype html><html lang="en"><head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${esc(title)} · Http11Probe</title>
<link rel="icon" href="data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><text y='.9em' font-size='90'>🔀</text></svg>">
<link rel="stylesheet" href="/assets/styles.css">${headExtra}
</head><body>
<header class="topbar">
  <button id="menu-toggle" class="icon-btn menu-toggle" aria-label="Menu">☰</button>
  <a class="brand" href="/">Http<b>11</b>Probe</a>
  <div class="search"><input id="search-input" type="search" placeholder="Search tests…" autocomplete="off" spellcheck="false">
    <div id="search-results" class="search-results"></div></div>
  <div class="grow"></div>
  <nav class="nav-links"><a href="${GH}">GitHub</a></nav>
  <button id="theme" class="icon-btn" aria-label="Toggle theme">◐</button>
</header>
<div class="scrim"></div>
<div class="layout">
  <aside class="sidebar">${navHtml}</aside>
  <main class="main${wide?" wide":""}">
    ${breadcrumb?`<div class="breadcrumb">${breadcrumb}</div>`:""}
    <div class="${contentClass}">${bodyHtml}</div>
    <div class="page-foot"><span>Http11Probe — HTTP/1.1 compliance &amp; smuggling tester</span><a href="${GH}">Source on GitHub</a></div>
  </main>
</div>
<script src="/search-index.js"></script>
<script src="/assets/nav.js"></script>${scripts}
</body></html>`;
}

function renderMarkdown(pg){
  const urlDir = pg.url.replace(/\/[^/]*$/,"").replace(/\/$/,"") || "";
  let html = md.render(preprocess(pg.body, urlDir||"/docs"));
  // mark the first table on glossary test pages as the metadata table
  if(pg.section==="Glossary" && !pg.isIndex) html = html.replace("<table>", '<table class="meta-table">');
  const heading = `<h1>${esc(pg.forceTitle||pg.title)}</h1>`;
  return heading + html;
}

function breadcrumbFor(pg){
  const parts=[`<a href="/">Home</a>`];
  if(pg.section==="Glossary"){ parts.push(`<a href="/docs/">Glossary</a>`); if(!pg.isIndex) parts.push(esc(pg.group)); }
  else if(pg.section==="Servers"){ parts.push(`<a href="/servers/">Servers</a>`); }
  else if(pg.section==="Overview"){ parts.push(`<a href="/docs/">Docs</a>`); }
  else parts.push(esc(pg.section));
  return parts.join(" › ");
}

// ---------- landing (matrix) ----------
function landing(navHtml){
  const body=`<div class="hero-lite">
    <h1>HTTP/1.1 behaviour, measured</h1>
    <p>Every framework, every probe, one grid. Green passes, amber warns (the RFC permits either), red fails.
       Pick the frameworks and categories to compare; click any cell to open that test's page.</p>
    <div class="provstrip" id="provstrip"></div>
  </div>
  <div class="toolbar">
    <div class="fw-dd" id="fw-dd">
      <button class="fw-trigger" id="fw-trigger" aria-expanded="false" aria-haspopup="true">
        <span>Frameworks</span><span class="selcount" id="selcount"></span><span class="caret">▾</span></button>
      <div class="fw-menu" id="fw-menu" hidden></div>
    </div>
    <div class="chips" id="chips"></div>
  </div>
  <div class="matrix-scroll"><table class="matrix" id="matrix"><thead id="mhead"></thead><tbody id="mbody"></tbody></table></div>
  <div class="rowcount" id="rowcount"></div>
  <div id="tip"></div>`;
  return shell({ title:"Results", navHtml, bodyHtml:body, wide:true, contentClass:"matrix-page",
    scripts:`\n<script src="/data.js"></script>\n<script src="/slugmap.js"></script>\n<script src="/assets/grid.js"></script>` });
}

function entropyPage(navHtml){
  const cfg={ cats:["Compliance","Smuggling","MalformedInput"], levels:["Must"],
    showCatChips:false, showEntropy:true, sort:"entropy" };
  const body=`<div class="hero-lite">
    <h1>Entropy</h1>
    <p>A matrix study of the <strong>MUST / MUST&nbsp;NOT</strong> tests in Compliance, Smuggling and
       Malformed&nbsp;Input. Each row's <em>entropy</em> is the Shannon entropy (in bits) of its verdict
       distribution across the selected frameworks: <strong>0</strong> means every framework agrees,
       higher means the field is split on a hard requirement. Rows are ranked most-contested first.</p>
    <div class="provstrip" id="provstrip"></div>
  </div>
  <div class="toolbar">
    <div class="fw-dd" id="fw-dd">
      <button class="fw-trigger" id="fw-trigger" aria-expanded="false" aria-haspopup="true">
        <span>Frameworks</span><span class="selcount" id="selcount"></span><span class="caret">▾</span></button>
      <div class="fw-menu" id="fw-menu" hidden></div>
    </div>
    <div class="chips" id="chips"></div>
  </div>
  <div class="matrix-scroll"><table class="matrix" id="matrix"><thead id="mhead"></thead><tbody id="mbody"></tbody></table></div>
  <div class="rowcount" id="rowcount"></div>
  <div id="tip"></div>`;
  return shell({ title:"Entropy", navHtml, bodyHtml:body, wide:true, contentClass:"matrix-page",
    scripts:`\n<script>window.GRID_CONFIG=${JSON.stringify(cfg)};</script>\n<script src="/data.js"></script>\n<script src="/slugmap.js"></script>\n<script src="/assets/grid.js"></script>` });
}

// ---------- slugmap + search index ----------
function buildSlugmap(pages){
  const map={};
  for(const pg of pages){ if(pg.isIndex) continue;   // scan every content page for a Test ID row
    const m=pg.body.match(/\*\*Test ID\*\*\s*\|\s*`([^`]+)`/);
    if(m) map[m[1].trim()]=pg.url;
  }
  return map;
}
function testIdOf(pg){ const m=pg.body.match(/\*\*Test ID\*\*\s*\|\s*`([^`]+)`/); return m?m[1].trim():null; }

// ---------- run ----------
function main(){
  if(exists(DIST)) fs.rmSync(DIST,{recursive:true});
  const pages=buildPages();
  const slugmap=buildSlugmap(pages);

  // per-page
  for(const pg of pages){
    let outRel = pg.url.endsWith("/") ? pg.url+"index.html" : pg.url;
    const out=path.join(DIST, outRel.replace(/^\//,""));
    ensure(out);
    const nav=buildNav(pages, pg.url);
    const html=shell({ title:pg.forceTitle||pg.title, navHtml:nav, bodyHtml:renderMarkdown(pg),
      breadcrumb:breadcrumbFor(pg), wide:pg.url==="/docs/rfc-requirement-dashboard.html" });
    fs.writeFileSync(out, html);
  }
  // landing + entropy study
  fs.writeFileSync(path.join(DIST,"index.html"), landing(buildNav(pages,"/")));
  fs.writeFileSync(path.join(DIST,"entropy.html"), entropyPage(buildNav(pages,"/entropy.html")));

  // search index
  const idx=pages.map(pg=>{ const id=pg.section==="Glossary"&&!pg.isIndex?testIdOf(pg):null;
    return { title:pg.forceTitle||pg.title, url:pg.url, id, cat:pg.group }; });
  fs.writeFileSync(path.join(DIST,"search-index.js"), "window.SEARCH_INDEX="+JSON.stringify(idx)+";");
  fs.writeFileSync(path.join(DIST,"slugmap.js"), "window.SLUGMAP="+JSON.stringify(slugmap)+";");

  // assets
  const adst=path.join(DIST,"assets"); fs.mkdirSync(adst,{recursive:true});
  for(const f of fs.readdirSync(ASSETS)) fs.copyFileSync(path.join(ASSETS,f), path.join(adst,f));

  // data.js: env override, local web/ copy, or the CI/pipeline location; else stub
  const dataCandidates=[
    process.env.PROBE_DATA_JS,
    path.resolve(__dir,"data.js"),
    path.resolve(__dir,"../docs/static/probe/data.js"),
  ].filter(Boolean);
  const src=dataCandidates.find(exists);
  if(src) fs.copyFileSync(src, path.join(DIST,"data.js"));
  else fs.writeFileSync(path.join(DIST,"data.js"),"window.PROBE_DATA={servers:[]};");
  // GitHub Pages custom domain (keeps www.http-probe.com pointed at this deploy)
  fs.writeFileSync(path.join(DIST,"CNAME"), "www.http-probe.com\n");

  console.log(`Built ${pages.length} pages + landing → dist/`);
  console.log(`  glossary test links: ${Object.keys(slugmap).length}  ·  search entries: ${idx.length}  ·  data.js: ${src?"copied":"STUB"}`);
}
main();
