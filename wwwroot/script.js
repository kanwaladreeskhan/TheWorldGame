// 1. Dynamic API URL (Matches your launchSettings.json port)
const API_URL = window.location.origin + "/api";

// Core Data Management
const getCurrentPlayer = () => JSON.parse(localStorage.getItem('currentPlayer'));

function updateUI() {
    const player = getCurrentPlayer();
    if (!player) return;

    // Har element ko update karein jahan IDs match hoti hain
    const nameEls = document.querySelectorAll('#playerName');
    const balanceEls = document.querySelectorAll('#balance');

    nameEls.forEach(el => el.textContent = player.Name || player.name);
    balanceEls.forEach(el => {
        const val = player.Balance !== undefined ? player.Balance : player.balance;
        el.textContent = Number(val).toLocaleString();
    });
}

// 2. Fetch Player Data from Server (Silent Update)
async function refreshPlayerData() {
    const player = getCurrentPlayer();
    if (!player) return;
    try {
        // ID ki jagah Name use karein kyunke Controller name mang raha hai
        const res = await fetch(`${API_URL}/player/${player.Name}`);
        if (!res.ok) throw new Error("Database sync failed");
        
        const updated = await res.json();
        localStorage.setItem('currentPlayer', JSON.stringify(updated));
        updateUI();
    } catch (err) { console.error("Sync Error:", err); }
}

// 3. Game Engine Trigger
async function processNextTurn() {
    try {
        const res = await fetch(`${API_URL}/Game/next-turn`, { method: 'POST' });
        if (res.ok) {
            alert("Turn Processed! AI has updated the market.");
            await refreshPlayerData();
            if (typeof loadMarket === "function") loadMarket();
            if (typeof loadInventory === "function") loadInventory();
        }
    } catch (err) { alert("Engine failed to respond."); }
}

// 4. Initial Player Loading (index.html only)
// 4. Initial Player Loading (index.html only)
async function loadPlayers() {
    const container = document.getElementById('playerSelect');
    if (!container) return;

    try {
        // Aapke controller ke mutabiq sahi route ye hai:
        const res = await fetch(`${API_URL}/player`); 
        
        if (!res.ok) throw new Error("API response was not OK");
        
        const players = await res.json();
        
        container.innerHTML = players.map(p => `
            <div class="col-md-4 mb-4">
                <div class="glass-card p-3 h-100 d-flex flex-column justify-content-between shadow-sm">
                    <h5 class="text-white">${p.Name || p.name}</h5>
                    <p class="text-info fw-bold mb-3">$${(p.Balance || p.balance || 0).toLocaleString()}</p>
                    <button class="btn btn-light btn-sm" onclick="selectPlayer(${JSON.stringify(p).replace(/"/g, '&quot;')})">
                        Select Nation
                    </button>
                </div>
            </div>
        `).join('');
    } catch (err) { 
        console.error("Failed to load players:", err);
        container.innerHTML = `<p class="text-danger">Error: Could not connect to Database. Check SSMS and dotnet run.</p>`;
    }
}

function selectPlayer(playerObj) {
    // Normalizing keys carefully
    const normalized = { 
        PlayerId: playerObj.PlayerId || playerObj.Id || playerObj.playerId, 
        Name: playerObj.Name || playerObj.name, 
        Balance: playerObj.Balance || playerObj.balance 
    };
    localStorage.setItem('currentPlayer', JSON.stringify(normalized));
    location.reload();
}

function logout() {
    localStorage.clear();
    location.href = "index.html";
}

// Navigation
const showMarket = () => location.href = 'market.html';
const showInventory = () => location.href = 'inventory.html';
const showLeaderboard = () => location.href = 'leaderboard.html';

// Page Load Controller
window.onload = () => {
    const player = getCurrentPlayer();
    const dashboard = document.getElementById('dashboard');
    const playerSelect = document.getElementById('playerSelectSection');

    if (player) {
        if (dashboard) {
            dashboard.classList.remove('d-none');
            if (playerSelect) playerSelect.classList.add('d-none');
        }
        updateUI();
        refreshPlayerData(); // Sync with DB
    } else {
        if (playerSelect) {
            playerSelect.classList.remove('d-none');
            loadPlayers();
        }
    }
};