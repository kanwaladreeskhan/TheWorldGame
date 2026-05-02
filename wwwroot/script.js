// 1. Dynamic API URL
const API_URL = window.location.origin + "/api";

// Core Data Management
const getCurrentPlayer = () => JSON.parse(localStorage.getItem('currentPlayer'));

function updateUI() {
    const player = getCurrentPlayer();
    if (!player) return;

    const nameEls = document.querySelectorAll('#playerName');
    const balanceEls = document.querySelectorAll('#balance');

    nameEls.forEach(el => el.textContent = player.Name || player.name);
    balanceEls.forEach(el => {
        const val = player.Balance !== undefined ? player.Balance : (player.balance || 0);
        el.textContent = Number(val).toLocaleString();
    });
}

// 2. Fetch Player Data from Server (Silent Update)
async function refreshPlayerData() {
    const player = getCurrentPlayer();
    if (!player) return;
    try {
        // Name-based sync as per your controller
        const res = await fetch(`${API_URL}/player/${player.Name}`);
        if (!res.ok) throw new Error("Database sync failed");
        
        const updated = await res.json();
        localStorage.setItem('currentPlayer', JSON.stringify(updated));
        updateUI();
    } catch (err) { console.error("Sync Error:", err); }
}

// 3. FIXED BUY/SELL LOGIC (Ye Missing Tha)
async function quickTrade(resourceId, action) {
    const player = getCurrentPlayer();
    if (!player) {
        alert("Pehle Nation select karein!");
        return;
    }

    try {
        // Note: Check your Controller if it expects /trade/execute or just /trade
        const res = await fetch(`${API_URL}/trade`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                PlayerId: player.PlayerId || player.Id,
                ResourceId: resourceId,
                Action: action, // 'BUY' or 'SELL'
                Quantity: 1
            })
        });

        const result = await res.json();

        if (res.ok) {
            console.log(`${action} Success:`, result);
            // 1. Player ka balance update karein
            await refreshPlayerData();
            // 2. Market ki supply update karein
            if (typeof loadMarket === "function") loadMarket();
            // 3. Inventory update karein
            if (typeof loadInventory === "function") loadInventory();
        } else {
            alert("Trade Failed: " + (result.message || "Insufficient funds or stock"));
        }
    } catch (err) {
        console.error("Trade Error:", err);
        alert("Server connection failed during trade.");
    }
}

// 4. Game Engine Trigger
async function processNextTurn() {
    // 1. LocalStorage se current player ki ID nikalna zaroori hai
    const player = JSON.parse(localStorage.getItem('currentPlayer'));
    
    if (!player) {
        alert("Pehle dashboard se apni nation select karein!");
        return;
    }

    try {
        console.log("Turn processing for Player ID:", player.PlayerId || player.Id);

        // 2. Ye fetch call Controller ko 'TurnRequest' bhej rahi hai
        const res = await fetch('/api/game/next-turn', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                // C# ke 'TurnRequest' model se match karne ke liye 'PlayerId' ka Capital P zaroori hai
                PlayerId: player.PlayerId || player.Id 
            })
        });

        if (res.ok) {
            const result = await res.json();
            alert(result.message); // "Turn X completed!" wala message dikhayega

            // 3. Turn ke baad prices aur wealth badal gayi hogi, isliye UI refresh
            await refreshPlayerData(); 
            if (typeof loadMarket === "function") await loadMarket();
            if (typeof loadLeaderboard === "function") await loadLeaderboard();
        } else {
            const error = await res.json();
            alert("Error: " + error.message);
        }
    } catch (err) {
        console.error("Fetch error:", err);
        alert("Game engine se rabta nahi ho pa raha.");
    }
}
// 5. Initial Player Loading
async function loadPlayers() {
    const container = document.getElementById('playerSelect');
    if (!container) return;

    try {
        const res = await fetch(`${API_URL}/player`); 
        if (!res.ok) throw new Error("API response was not OK");
        
        const players = await res.json();
        
        container.innerHTML = players.map(p => `
            <div class="col-md-4 mb-4">
                <div class="glass-card p-3 h-100 d-flex flex-column justify-content-between shadow-sm">
                    <h5 class="text-white text-uppercase fw-bold">${p.Name || p.name}</h5>
                    <p class="text-info fw-bold mb-3">$${(p.Balance || p.balance || 0).toLocaleString()}</p>
                    <button class="btn btn-outline-light btn-sm rounded-pill" onclick="selectPlayer(${JSON.stringify(p).replace(/"/g, '&quot;')})">
                        Select Nation
                    </button>
                </div>
            </div>
        `).join('');
    } catch (err) { 
        console.error("Failed to load players:", err);
        container.innerHTML = `<div class="alert alert-danger">Database Connection Error. Check SSMS.</div>`;
    }
}

function selectPlayer(playerObj) {
    const normalized = { 
        Id: playerObj.Id || playerObj.PlayerId || playerObj.id, 
        PlayerId: playerObj.Id || playerObj.PlayerId || playerObj.id,
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

// Navigation Functions
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
        refreshPlayerData(); 
    } else {
        if (playerSelect) {
            playerSelect.classList.remove('d-none');
            loadPlayers();
        }
    }
};
// ... baqi code (loadPlayers, updateUI wagera) ...

async function quickTrade(resourceId, action) {
    console.log("Trading started:", resourceId, action);
    const player = getCurrentPlayer();
    
    if (!player) {
        alert("Pehle Nation select karein!");
        return;
    }

    try {
        const res = await fetch(`${API_URL}/trade`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                PlayerId: player.PlayerId || player.Id,
                ResourceId: resourceId,
                Action: action,
                Quantity: 1
            })
        });

       if (res.ok) {
            console.log(`${action} successful. Syncing data...`);
            
            // 1. Pehle Backend se fresh balance aur inventory data lein
            await refreshPlayerData(); 
            
            // 2. Agar Market Terminal khula hai to usay refresh karein (Supply update hogi)
            if (typeof loadMarket === "function") loadMarket(); 
            
            // 3. CRITICAL: Agar Leaderboard widget page par hai to usay refresh karein
            if (typeof loadLeaderboard === "function") {
                await loadLeaderboard(); 
            } else {
                // Agar function nahi mil raha to silent refresh ya console log
                console.log("Leaderboard function not found on this page.");
            }

            alert(action + " Successful!");
        } else {
            const error = await res.json();
            alert("Error: " + (error.message || "Trade failed"));
        }
    } catch (err) {
        console.error("Fetch error:", err);
    }
}