const API_URL = window.location.origin + "/api";

// Name helpers
function cleanName(raw) {
    let n = raw.replace(/AI$/i, '');
    if (n === "Player1") n = "Pakistan";
    return n;
}
function getFlagCode(c) {
    const m = {"Pakistan":"pk","USA":"us","China":"cn","Germany":"de","Japan":"jp","India":"in","UK":"gb","Russia":"ru","UAE":"ae"};
    return m[c] || "un";
}
function getFlagUrl(c) { return `https://flagcdn.com/w80/${getFlagCode(cleanName(c))}.png`; }
const getCurrentPlayer = () => JSON.parse(localStorage.getItem('currentPlayer'));

function updateUI() {
    const p = getCurrentPlayer();
    if (!p) return;
    document.querySelectorAll('#playerName').forEach(el => el.textContent = cleanName(p.Name || p.name));
    document.querySelectorAll('#balance').forEach(el => el.textContent = '$' + (p.Balance || 0).toLocaleString());
}

async function refreshPlayerData() {
    const p = getCurrentPlayer();
    if (!p) return;
    const res = await fetch(`${API_URL}/player/${encodeURIComponent(p.Name)}`);
    if (res.ok) {
        const u = await res.json();
        localStorage.setItem('currentPlayer', JSON.stringify(u));
        updateUI();
    }
}

async function updateGameStatus() {
    try {
        const res = await fetch('/api/game/state');
        const state = await res.json();
        const badge = document.getElementById('gameModeBadge');
        const startBtn = document.getElementById('startWarBtn');
        const endBtn = document.getElementById('endWarBtn');
        document.getElementById('turnNumber').innerText = state.turnNumber || 1;
        if (state.mode === "War") {
            badge.innerText = "WAR ACTIVE"; badge.className = "badge bg-danger fs-5";
            document.body.classList.add('war-theme');
            startBtn?.classList.add('d-none'); endBtn?.classList.remove('d-none');
            if (window.showWarSign) window.showWarSign(true);
        } else {
            badge.innerText = "Normal"; badge.className = "badge bg-success fs-5";
            document.body.classList.remove('war-theme');
            startBtn?.classList.remove('d-none'); endBtn?.classList.add('d-none');
            if (window.showWarSign) window.showWarSign(false);
        }
    } catch (e) { console.error(e); }
}

// ---------- TRADE (with line) ----------
async function tradeWithCountry(targetCountryName, resourceId, action, quantity, resourceName) {
    const p = getCurrentPlayer();
    if (!p) return alert("Select your nation first!");
    try {
        const res = await fetch(`${API_URL}/trade`, {
            method: 'POST',
            headers: {'Content-Type':'application/json'},
            body: JSON.stringify({
                PlayerId: p.PlayerId || p.Id,
                ResourceId: resourceId,
                Action: action,
                Quantity: quantity
            })
        });
        const result = await res.json();
        if (!res.ok) throw new Error(result.message || 'Trade failed');

        alert(`✅ Trade successful!\n${action} ${quantity} ${resourceName} with ${targetCountryName}`);

        const myName = cleanName(p.Name);
        const myId = window.getCountryIdByName(myName);
        const targetId = window.getCountryIdByName(targetCountryName);
        const scene = window.mainScene;
        if (myId && targetId && scene && window.showTradeLine) {
            const from = (action === 'BUY') ? targetId : myId;
            const to   = (action === 'BUY') ? myId : targetId;
            window.showTradeLine(scene, from, to, resourceName);
        }
        await refreshPlayerData();
        if (window.updateMapGlow) window.updateMapGlow();
    } catch (e) { alert(e.message); }
}

function openTradePopup(targetCountryName) {
    document.getElementById('targetCountry').innerText = targetCountryName;
    const listDiv = document.getElementById('trade-resource-list');
    fetch(`${API_URL}/market`)
        .then(r => r.json())
        .then(market => {
            listDiv.innerHTML = market.map(m => `
                <div class="col-6 mb-2">
                    <div class="cyber-card p-2 text-center">
                        <strong>${m.Name}</strong>
                        <div class="text-success">$${m.CurrentPrice?.toFixed(2)}</div>
                        <div class="d-flex justify-content-center mt-2">
                            <input type="number" id="qty-${m.ResourceId || m.Id}" class="form-control form-control-sm bg-dark text-white me-2" style="width:60px;" value="1" min="1">
                            <button class="btn btn-sm btn-success me-1" onclick="tradeWithCountry('${targetCountryName}', ${m.ResourceId || m.Id}, 'BUY', parseInt(document.getElementById('qty-${m.ResourceId || m.Id}').value), '${m.Name}')">BUY</button>
                            <button class="btn btn-sm btn-danger" onclick="tradeWithCountry('${targetCountryName}', ${m.ResourceId || m.Id}, 'SELL', parseInt(document.getElementById('qty-${m.ResourceId || m.Id}').value), '${m.Name}')">SELL</button>
                        </div>
                    </div>
                </div>
            `).join('');
        });
    new bootstrap.Modal(document.getElementById('tradePopup')).show();
}

async function processNextTurn() {
    const p = getCurrentPlayer();
    if (!p) return alert("Select a nation first!");
    document.body.style.transform = 'translateX(5px)';
    setTimeout(() => document.body.style.transform = 'translateX(-5px)', 80);
    setTimeout(() => document.body.style.transform = 'translateX(0)', 160);
    if (window.animateNextTurn) window.animateNextTurn();
    const res = await fetch('/api/game/next-turn', {
        method: 'POST',
        headers: {'Content-Type':'application/json'},
        body: JSON.stringify({ PlayerId: p.PlayerId || p.Id })
    });
    if (res.ok) {
        const result = await res.json();
        showNews(result.events?.join(' | ') || "Markets updated");
        await refreshPlayerData();
        if (window.updateMapGlow) window.updateMapGlow();
        updateGameStatus();
    } else { alert('Turn failed'); }
}

async function startWar() {
    const res = await fetch('/api/game/start-war', { method:'POST' });
    if (res.ok) { window.triggerWarExplosion?.(); updateGameStatus(); showNews("⚠️ WAR HAS BEGUN!"); }
}
async function endWar() {
    const res = await fetch('/api/game/end-war', { method:'POST' });
    if (res.ok) { updateGameStatus(); showNews("🕊️ Peace restored."); }
}

function showNews(msg) {
    const ticker = document.getElementById('news-ticker');
    const text = document.getElementById('ticker-text');
    if (!ticker || !text) return;
    ticker.classList.remove('d-none');
    text.innerText = msg;
}

// Player selection
async function loadPlayers() {
    const container = document.getElementById('playerSelect');
    if (!container) return;
    const res = await fetch(`${API_URL}/player`);
    if (!res.ok) { container.innerHTML = '<div class="alert alert-danger">Server error.</div>'; return; }
    let players = await res.json();
    players = players.map(p => { if (p.Name === "Player1") p.Name = "Pakistan"; return p; });
    container.innerHTML = players.map(p => `
        <div class="col-md-4 mb-4">
            <div class="cyber-card p-3 text-center">
                <img src="${getFlagUrl(p.Name)}" style="width:70px; filter:drop-shadow(0 0 6px #00d2ff);">
                <h4 class="text-white fw-bold">${cleanName(p.Name)}</h4>
                <p class="text-info">$${(p.Balance || 0).toLocaleString()}</p>
                <button class="btn btn-outline-light btn-sm rounded-pill" onclick="selectPlayer(${JSON.stringify(p).replace(/"/g,'&quot;')})">Select Nation</button>
            </div>
        </div>
    `).join('');
}
function selectPlayer(p) {
    localStorage.setItem('currentPlayer', JSON.stringify({
        Id: p.Id || p.PlayerId || p.id,
        PlayerId: p.Id || p.PlayerId || p.id,
        Name: p.Name,
        Balance: p.Balance || 0
    }));
    location.reload();
}
function logout() { localStorage.clear(); location.href = "game.html"; }

window.onload = () => {
    const p = getCurrentPlayer();
    if (p) {
        document.getElementById('dashboard').classList.remove('d-none');
        document.getElementById('playerSelectSection').classList.add('d-none');
        updateUI();
        refreshPlayerData();
        updateGameStatus();
        if (window.initPhaser) window.initPhaser();
    } else {
        document.getElementById('playerSelectSection').classList.remove('d-none');
        loadPlayers();
    }
};