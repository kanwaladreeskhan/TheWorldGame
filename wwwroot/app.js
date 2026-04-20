const API_URL = "/api";

// Navigation functions
function showMarket() { window.location.href = 'market.html'; }
function showInventory() { window.location.href = 'inventory.html'; }
function showLeaderboard() { window.location.href = 'leaderboard.html'; }

// 1. Safe Display Balance
// ====================== FIXED BALANCE UPDATE ======================
function updateBalanceUI() {
    const player = getCurrentPlayer();
    if (!player) return;

    const balanceElements = document.querySelectorAll('#balance');

    balanceElements.forEach(el => {
        const amount = player.Balance || player.balance || 0;
        el.textContent = Number(amount).toLocaleString();
    });
}

// 2. Load Players on index.html
async function loadPlayers() {
    try {
        const res = await fetch(`${API_URL}/player`);
        const players = await res.json();
        const container = document.getElementById('playerSelect');
        if (!container) return;

        container.innerHTML = players.map(p => `
            <div class="col-md-12 mb-2">
                <button class="btn btn-light w-100 text-dark py-2 shadow-sm" 
                        onclick="selectPlayer(${p.PlayerId || p.Id}, '${p.Name}', ${p.Balance})">
                    ${p.Name} - $${(p.Balance || 0).toLocaleString()}
                </button>
            </div>
        `).join('');
    } catch (err) { console.error("Error loading players:", err); }
}
function getCurrentPlayer() {
    const saved = localStorage.getItem('currentPlayer');
    return saved ? JSON.parse(saved) : null;
}
// 3. Select Player Logic
function selectPlayer(id, name, balance) {
    const player = { PlayerId: id, Name: name, Balance: balance };
    localStorage.setItem('currentPlayer', JSON.stringify(player));
    window.location.href = "index.html";
}

// 4. Logout (Exit)
function logout() {
    localStorage.removeItem('currentPlayer');
    window.location.href = "index.html";
}

// 5. Initialize on Page Load
window.onload = () => {
    const saved = localStorage.getItem('currentPlayer');
    const player = saved ? JSON.parse(saved) : null;

    if (player) {
        // Agar dashboard page par hain
        if (document.getElementById('dashboard')) {
            document.getElementById('playerSelect').classList.add('d-none');
            document.getElementById('dashboard').classList.remove('d-none');
            document.getElementById('playerName').textContent = "Welcome, " + player.Name;
            updateBalanceUI(player.Balance);
        }
        // Navbar updates for other pages
        if (document.getElementById('playerName') && !document.getElementById('dashboard')) {
            document.getElementById('playerName').textContent = player.Name;
            updateBalanceUI(player.Balance);
        }
    } else {
        if (document.getElementById('playerSelect')) loadPlayers();
    }
   
};