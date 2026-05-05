const API_URL = window.location.origin + "/api";

// ========== SOUND ENGINE ==========
let audioCtx = null;
function initAudio() { if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)(); }
function playBeep(freq, dur, type = 'sine', vol = 0.3) {
    if (!audioCtx) return;
    const osc = audioCtx.createOscillator();
    const gain = audioCtx.createGain();
    osc.type = type; osc.frequency.setValueAtTime(freq, audioCtx.currentTime);
    gain.gain.setValueAtTime(vol, audioCtx.currentTime);
    osc.connect(gain); gain.connect(audioCtx.destination);
    osc.start(); osc.stop(audioCtx.currentTime + dur);
}
function playTradeSound() { initAudio(); playBeep(800, 0.1); playBeep(1000, 0.08); }
function playWarSound() { initAudio(); playBeep(200, 0.3, 'sawtooth', 0.4); playBeep(150, 0.4, 'sawtooth', 0.4); }
function playNextTurnSound() { initAudio(); playBeep(500, 0.15, 'triangle'); playBeep(700, 0.1, 'triangle'); }

// ========== NAME HELPERS ==========
function cleanName(raw) {
    let n = raw.replace(/AI$/i, '');
    if (n === "Player1") n = "Pakistan";
    return n;
}
function getFlagCode(c) { const m = {"Pakistan":"pk","USA":"us","China":"cn","Germany":"de","Japan":"jp","India":"in","UK":"gb","Russia":"ru","UAE":"ae"}; return m[c] || "un"; }
function getFlagUrl(c) { return `https://flagcdn.com/w80/${getFlagCode(cleanName(c))}.png`; }
const getCurrentPlayer = () => JSON.parse(localStorage.getItem('currentPlayer'));

// ========== UI ==========
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
    if (res.ok) { const u = await res.json(); localStorage.setItem('currentPlayer', JSON.stringify(u)); updateUI(); }
}

// ========== TRADE LOG ==========
const tradeLog = [];
function addTradeLogEntry(from, to, resource, action, qty) {
    tradeLog.unshift({ time: new Date().toLocaleTimeString(), from: cleanName(from), to: cleanName(to), resource, action, qty });
    if (tradeLog.length > 20) tradeLog.pop();
    renderTradeLog();
}
function renderTradeLog() {
    const logDiv = document.getElementById('tradeLogEntries');
    if (!logDiv) return;
    logDiv.innerHTML = tradeLog.map(e => `
        <div class="trade-log-entry">
            <span class="text-muted">${e.time}</span> –
            ${e.action==='BUY'?'🟢':'🔴'} <b>${e.from}</b> → <b>${e.to}</b>: ${e.qty} ${e.resource}
        </div>
    `).join('');
}

// ========== MINI RANKING ==========
async function updateMiniRanking() {
    const mini = document.getElementById('miniRanking');
    if (!mini) return;
    try {
        const res = await fetch('/api/player/leaderboard');
        const data = await res.json();
        const top3 = data.slice(0, 3);
        mini.innerHTML = top3.map((p,i) => {
            const medal = ['🥇','🥈','🥉'][i];
            const name = cleanName(p.Name || p.name);
            const wealth = Math.floor(p.TotalWealth || p.totalWealth || 0).toLocaleString();
            return `<div>${medal} ${name} – $${wealth}</div>`;
        }).join('');
    } catch(e) { console.error(e); }
}

// ========== FULL RANKING MODAL ==========
async function showFullRanking() {
    const body = document.getElementById('fullRankingBody');
    try {
        const res = await fetch('/api/player/leaderboard');
        const data = await res.json();
        body.innerHTML = `
            <table class="table table-dark table-striped">
                <thead><tr><th>#</th><th>Nation</th><th class="text-end">Wealth</th></tr></thead>
                <tbody>
                    ${data.map((p,i) => `
                        <tr class="${cleanName(p.Name) === cleanName(getCurrentPlayer()?.Name) ? 'table-active' : ''}">
                            <td>${i+1}</td>
                            <td>${cleanName(p.Name)}</td>
                            <td class="text-end">$${Math.floor(p.TotalWealth || 0).toLocaleString()}</td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>`;
        new bootstrap.Modal(document.getElementById('fullRankingModal')).show();
    } catch(e) { console.error(e); }
}

// ========== WINNER ==========
function getFlagEmoji(country) {
    const m = {"Pakistan":"🇵🇰","USA":"🇺🇸","China":"🇨🇳","Germany":"🇩🇪","Japan":"🇯🇵","India":"🇮🇳","UK":"🇬🇧","Russia":"🇷🇺","UAE":"🇦🇪"};
    return m[country]||"🏳️";
}
async function checkWinner() {
    try {
        const res = await fetch('/api/player/leaderboard');
        const data = await res.json();
        if (data.length === 0) return;
        const top = data[0];
        document.getElementById('winnerFlag').textContent = getFlagEmoji(cleanName(top.Name));
        document.getElementById('winnerName').textContent = cleanName(top.Name);
        document.getElementById('winnerWealth').textContent = '$' + Math.floor(top.TotalWealth || 0).toLocaleString();
        new bootstrap.Modal(document.getElementById('winnerModal')).show();
        playTradeSound();
    } catch(e) { console.error(e); }
}

// ========== GAME STATE ==========
async function updateGameStatus() {
    try {
        const res = await fetch('/api/game/state');
        const state = await res.json();
        document.getElementById('turnNumber').innerText = state.turnNumber || 1;
        const badge = document.getElementById('gameModeBadge');
        const startBtn = document.getElementById('startWarBtn'), endBtn = document.getElementById('endWarBtn');
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

// ========== WAR INVENTORY POPUP ==========
async function showWarInventory() {
    const player = getCurrentPlayer();
    if (!player) return;
    try {
        const [invRes, marketRes] = await Promise.all([
            fetch(`${API_URL}/player/${player.Id}/inventory`),
            fetch(`${API_URL}/market`)
        ]);
        const inventory = await invRes.json();
        const market = await marketRes.json();
        const body = document.getElementById('warInventoryBody');
        let html = `<p class="mb-2">Current holdings for <b>${cleanName(player.Name)}</b> during wartime:</p>`;
        html += `<ul class="list-group">`;
        inventory.forEach(item => {
            const mkt = market.find(m => m.ResourceId === item.ResourceId || m.Id === item.ResourceId);
            const price = mkt ? mkt.CurrentPrice : 0;
            html += `<li class="list-group-item bg-dark text-white d-flex justify-content-between">
                <span>${item.ResourceName}</span>
                <span>${item.Quantity} units ($${(price * item.Quantity).toFixed(0)})</span>
            </li>`;
        });
        html += `</ul>`;
        body.innerHTML = html;
        new bootstrap.Modal(document.getElementById('warInventoryModal')).show();
    } catch(e) { console.error(e); }
}

// ========== TRADE ==========
async function tradeWithCountry(targetCountryName, resourceId, action, quantity, resourceName) {
    const player = getCurrentPlayer();
    if (!player) return alert("Select your nation first!");
    try {
        const res = await fetch(`${API_URL}/trade`, {
            method: 'POST', headers: {'Content-Type':'application/json'},
            body: JSON.stringify({ PlayerId: player.PlayerId || player.Id, ResourceId: resourceId, Action: action, Quantity: quantity })
        });
        const result = await res.json();
        if (!res.ok) throw new Error(result.message || 'Trade failed');

        playTradeSound();
        addTradeLogEntry(cleanName(player.Name), targetCountryName, resourceName, action, quantity);
        alert(`✅ Trade successful!\n${action} ${quantity} ${resourceName} with ${targetCountryName}`);

        const myId = window.getCountryIdByName?.(cleanName(player.Name));
        const targetId = window.getCountryIdByName?.(targetCountryName);
        const scene = window.mainScene;
        if (myId && targetId && scene && window.showTradeLine) {
            const from = (action === 'BUY') ? targetId : myId;
            const to   = (action === 'BUY') ? myId : targetId;
            window.showTradeLine(scene, from, to, resourceName);
        }
        await refreshPlayerData();
        if (window.updateMapGlow) window.updateMapGlow();
        updateMiniRanking();
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
                </div>`).join('');
        });
    new bootstrap.Modal(document.getElementById('tradePopup')).show();
}

// ========== NEXT TURN ==========
async function processNextTurn() {
    const player = getCurrentPlayer();
    if (!player) return alert("Select a nation first!");
    playNextTurnSound();
    document.body.style.transform = 'translateX(5px)';
    setTimeout(() => document.body.style.transform = 'translateX(-5px)', 80);
    setTimeout(() => document.body.style.transform = 'translateX(0)', 160);
    if (window.animateNextTurn) window.animateNextTurn();
    const res = await fetch('/api/game/next-turn', {
        method: 'POST', headers: {'Content-Type':'application/json'},
        body: JSON.stringify({ PlayerId: player.PlayerId || player.Id })
    });
    if (res.ok) {
        const result = await res.json();
        showNews(result.events?.join(' | ') || "Markets updated");
        // Simulated AI trades
        const aiCountries = ["USA","China","Germany","Japan","India","UK","Russia","UAE"];
        const resources = ["Oil","Gold","Food","Steel","Technology","Gas","Coal","Electronics"];
        for (let i=0; i<3; i++) {
            const from = aiCountries[Math.floor(Math.random()*aiCountries.length)];
            let to = aiCountries[Math.floor(Math.random()*aiCountries.length)];
            while (to === from) to = aiCountries[Math.floor(Math.random()*aiCountries.length)];
            addTradeLogEntry(from, to, resources[Math.floor(Math.random()*resources.length)], 'BUY', Math.floor(Math.random()*5)+1);
        }
        await refreshPlayerData();
        if (window.updateMapGlow) window.updateMapGlow();
        updateGameStatus();
        updateMiniRanking();
        const updatedPlayer = getCurrentPlayer();
        if (updatedPlayer && updatedPlayer.Balance > 20000) checkWinner();
    } else { alert('Turn failed'); }
}

// ========== WAR (with inventory popup) ==========
async function startWar() {
    const res = await fetch('/api/game/start-war', { method:'POST' });
    if (res.ok) {
        playWarSound();
        window.triggerWarExplosion?.();
        updateGameStatus();
        showNews("⚠️ WAR HAS BEGUN! Prices will spike.");
        showWarInventory();  // <-- show inventory popup
    }
}
async function endWar() {
    const res = await fetch('/api/game/end-war', { method:'POST' });
    if (res.ok) {
        updateGameStatus();
        showNews("🕊️ Peace restored.");
        showWarInventory();  // <-- also show inventory when peace returns
    }
}

function showNews(msg) {
    const ticker = document.getElementById('news-ticker');
    const text = document.getElementById('ticker-text');
    if (!ticker || !text) return;
    ticker.classList.remove('d-none');
    text.innerText = msg;
}

// ========== PLAYER SELECTION ==========
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
        </div>`).join('');
}
function selectPlayer(p) {
    localStorage.setItem('currentPlayer', JSON.stringify({
        Id: p.Id || p.PlayerId || p.id, PlayerId: p.Id || p.PlayerId || p.id,
        Name: p.Name, Balance: p.Balance || 0
    }));
    location.reload();
}
function logout() { localStorage.clear(); location.href = "game.html"; }

// ========== PAGE INIT ==========
window.onload = () => {
    const player = getCurrentPlayer();
    const dash = document.getElementById('dashboard');
    const selectSec = document.getElementById('playerSelectSection');
    const tradeLogCont = document.getElementById('tradeLogContainer');
    const winnerBtn = document.getElementById('winnerBtn');

    if (player) {
        dash?.classList.remove('d-none');
        selectSec?.classList.add('d-none');
        tradeLogCont?.classList.remove('d-none');   // show sidebar
        winnerBtn?.classList.remove('d-none');      // show winner button
        updateUI();
        refreshPlayerData();
        updateGameStatus();
        updateMiniRanking();
        if (window.initPhaser) window.initPhaser();
    } else {
        selectSec?.classList.remove('d-none');
        loadPlayers();
    }
};