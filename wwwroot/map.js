window.countries = {};
let mainScene = null;
let glowGraphics = null;
let warSignText = null;

const MAP_DATA = [
    { id:1, name:"Pakistan", x:750, y:320, isPlayer:true },
    { id:2, name:"USA",     x:200, y:180 },
    { id:3, name:"China",   x:900, y:220 },
    { id:4, name:"Germany", x:580, y:150 },
    { id:5, name:"Japan",   x:1000, y:260 },
    { id:6, name:"UK",      x:500, y:130 },
    { id:7, name:"Russia",  x:800, y:100 },
    { id:8, name:"UAE",     x:700, y:380 }
];

window.getCountryIdByName = (name) => {
    const c = MAP_DATA.find(c => c.name === name);
    return c ? c.id : null;
};

function getFlagCode(n) {
    const m = {"Pakistan":"pk","USA":"us","China":"cn","Germany":"de","Japan":"jp","India":"in","UK":"gb","Russia":"ru","UAE":"ae"};
    return m[n]||"un";
}

window.initPhaser = function() {
    if (window.gameInstance) return;
    const config = {
        type: Phaser.AUTO,
        width: 1200, height: 500,
        parent: 'game-container',
        backgroundColor: '#000',
        scene: { preload, create }
    };
    window.gameInstance = new Phaser.Game(config);
};

function preload() {
    this.load.image('worldMap', 'assets/world-map-dark.jpg');
    MAP_DATA.forEach(c => this.load.image(`flag_${c.id}`, `https://flagcdn.com/w80/${getFlagCode(c.name)}.png`));
}

function create() {
    mainScene = this;
    window.mainScene = this;

    if (this.textures.exists('worldMap')) {
        this.add.image(600, 250, 'worldMap').setAlpha(0.85).setDepth(0);
    } else {
        const gfx = this.add.graphics();
        gfx.lineStyle(1, 0x1a3a5a, 0.3);
        for (let i=0; i<1200; i+=60) { gfx.moveTo(i,0); gfx.lineTo(i,500); }
        for (let j=0; j<500; j+=60) { gfx.moveTo(0,j); gfx.lineTo(1200,j); }
        gfx.strokePath();
    }

    glowGraphics = this.add.graphics().setDepth(1);

    warSignText = this.add.text(600, 30, '', {
        fontSize: '28px', fontStyle:'bold', color:'#ff0000',
        stroke:'#000', strokeThickness:5,
        shadow:{ blur:10, color:'#ff0000', fill:true }
    }).setOrigin(0.5).setDepth(20).setVisible(false);

    MAP_DATA.forEach(c => {
        const img = this.add.image(c.x, c.y, `flag_${c.id}`).setScale(0.9).setInteractive();
        img.setDepth(2);
        img.on('pointerover', () => showTooltip(c));
        img.on('pointerout', hideTooltip);
        img.on('pointerdown', () => {
            if (c.name !== getCurrentPlayerName()) openTradePopup(c.name);
        });
        window.countries[c.id] = { sprite: img, x: c.x, y: c.y, name: c.name };
    });

    updateMapGlow();
}

window.showWarSign = (active) => {
    if (!warSignText || !mainScene) return;
    if (active) {
        warSignText.setText('⚔️ OIL CRISIS ⚔️');
        warSignText.setVisible(true);
        mainScene.tweens.add({ targets: warSignText, scaleX:1.2, scaleY:1.2, duration:500, yoyo:true, repeat:-1 });
    } else {
        warSignText.setVisible(false);
        mainScene.tweens.killTweensOf(warSignText);
        warSignText.setScale(1);
    }
};

window.animateNextTurn = () => {
    if (!mainScene) return;
    const ids = Object.keys(window.countries);
    if (ids.length < 2) return;
    for (let i=0; i<3; i++) {
        const from = ids[Math.floor(Math.random()*ids.length)];
        let to = ids[Math.floor(Math.random()*ids.length)];
        while (to === from) to = ids[Math.floor(Math.random()*ids.length)];
        setTimeout(() => window.showTradeLine(mainScene, parseInt(from), parseInt(to), ['Oil','Gold','Steel','Food','Technology'][Math.floor(Math.random()*5)]), i*300);
    }
    mainScene.cameras.main.shake(250, 0.03);
};

window.showTradeLine = (scene, fromId, toId, resource) => {
    if (!scene) return;
    const from = window.countries[fromId], to = window.countries[toId];
    if (!from || !to) return;
    const gfx = scene.add.graphics();
    gfx.lineStyle(3, 0x00ffff, 0.9);
    gfx.beginPath(); gfx.moveTo(from.x, from.y); gfx.lineTo(to.x, to.y);
    gfx.strokePath();
    const dot = scene.add.circle(from.x, from.y, 5, 0xffffff).setDepth(5);
    scene.tweens.add({
        targets: dot, x: to.x, y: to.y, duration:2000, ease:'Sine.easeInOut',
        onUpdate: () => {
            const trail = scene.add.circle(dot.x, dot.y, 2, 0x00ffff, 0.7);
            scene.tweens.add({ targets: trail, alpha:0, scale:2, duration:300, onComplete:() => trail.destroy() });
        },
        onComplete: () => { dot.destroy(); gfx.destroy(); }
    });
    const label = scene.add.text((from.x+to.x)/2, (from.y+to.y)/2-15, resource, {
        fontSize:'16px', fontStyle:'bold', color:'#ffd700', stroke:'#000', strokeThickness:3
    }).setOrigin(0.5).setDepth(10);
    scene.tweens.add({ targets: label, alpha:0, duration:2500, onComplete:() => label.destroy() });
};

window.triggerWarExplosion = () => {
    if (!mainScene) return;
    const ids = Object.keys(window.countries);
    if (ids.length === 0) return;
    const randomId = ids[Math.floor(Math.random()*ids.length)];
    window.showWarExplosion(mainScene, window.countries[randomId].x, window.countries[randomId].y);
    setTimeout(() => {
        const id2 = ids[Math.floor(Math.random()*ids.length)];
        window.showWarExplosion(mainScene, window.countries[id2].x, window.countries[id2].y);
    }, 500);
};

window.showWarExplosion = (scene, x, y) => {
    if (!scene) return;
    scene.cameras.main.shake(500, 0.05);
    for (let i=0; i<5; i++) {
        const ring = scene.add.circle(x, y, 10, Phaser.Display.Color.GetColor(255, 100+i*30, 0), 0.8).setDepth(10);
        scene.tweens.add({ targets: ring, scaleX:5, scaleY:5, alpha:0, duration:600+i*100, delay:i*70, onComplete:() => ring.destroy() });
    }
};

let tooltip = null;
function showTooltip(c) {
    if (!mainScene) return;
    fetch('/api/player/leaderboard').then(r=>r.json()).then(data => {
        const entry = data.find(d => cleanName(d.Name) === c.name);
        const gdp = entry ? '$'+Math.floor(entry.TotalWealth).toLocaleString() : '...';
        if (tooltip) tooltip.destroy();
        tooltip = mainScene.add.text(c.x, c.y-70, `${c.name}\nGDP: ${gdp}`, {
            fontSize:'13px', color:'#fff', backgroundColor:'#000', padding:{x:8,y:5}
        }).setOrigin(0.5).setDepth(20);
    });
}
function hideTooltip() { if (tooltip) { tooltip.destroy(); tooltip = null; } }
function getCurrentPlayerName() {
    const p = JSON.parse(localStorage.getItem('currentPlayer'));
    return p ? cleanName(p.Name) : '';
}

async function updateMapGlow() {
    if (!glowGraphics) return;
    glowGraphics.clear();
    try {
        const res = await fetch('/api/player/leaderboard');
        const data = await res.json();
        const maxWealth = Math.max(...data.map(d => d.TotalWealth || 0), 1);
        data.forEach(entry => {
            const c = MAP_DATA.find(m => m.name === cleanName(entry.Name));
            if (!c) return;
            const intensity = Math.min(1, (entry.TotalWealth || 0) / maxWealth);
            const color = Phaser.Display.Color.GetColor(Math.floor(255*(1-intensity)), Math.floor(255*intensity), 50);
            glowGraphics.fillStyle(color, 0.25+intensity*0.3);
            glowGraphics.fillCircle(c.x, c.y, 22+intensity*18);
        });
    } catch(e) {}
}
window.updateMapGlow = updateMapGlow;