// Global storage & scene reference
window.countries = {};
let mainScene = null;

const config = {
    type: Phaser.AUTO,
    width: 1200,
    height: 700,
    parent: 'game-container',
    backgroundColor: '#1a1a2e',
    scene: {
        preload: preload,
        create: create,
        update: update
    }
};

const game = new Phaser.Game(config);

function preload() {
    // Load images if they exist, otherwise they'll fail silently, we handle gracefully
    this.load.image('worldMap', 'assets/world-map.jpg');
    this.load.image('flag_player', 'assets/flag-blue.png');
    this.load.image('flag_ai', 'assets/flag-red.png');
}

function create() {
    mainScene = this;

    // World map background
    this.add.image(600, 350, 'worldMap').setScale(0.8);

    // Fetch country positions from backend
    fetch('/api/game/mapdata')
        .then(res => res.json())
        .then(data => {
            data.forEach(c => {
                const key = c.isPlayer ? 'flag_player' : 'flag_ai';
                const sprite = this.add.image(c.x, c.y, key).setScale(0.15).setInteractive();
                sprite.on('pointerover', () => console.log(c.name));
                window.countries[c.id] = { sprite, x: c.x, y: c.y, name: c.name };
            });
        })
        .catch(err => console.error('Mapdata fetch failed:', err));

    // SignalR connection for real-time events
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/gamehub")
        .build();

    connection.on("TradeOccurred", (from, to, resource) => {
        if (!mainScene) return;
        window.showTradeLine(mainScene, from, to, resource);
    });

    connection.on("WarStarted", (attacker, defender, x, y) => {
        if (!mainScene) return;
        window.showWarExplosion(mainScene, x, y);
    });

    connection.start().catch(err => console.error('SignalR error:', err));

    // Keyboard shortcuts (T = trade, W = war)
    this.input.keyboard.on('keydown-T', () => {
        if (window.countries[1] && window.countries[2])
            window.showTradeLine(this, 1, 2, "Oil");
    });
    this.input.keyboard.on('keydown-W', () => {
        window.showWarExplosion(this, 220, 200);
    });
}

function update() {}

// Trade line animation
window.showTradeLine = function (scene, fromId, toId, resource) {
    const from = window.countries[fromId];
    const to = window.countries[toId];
    if (!from || !to || !scene) return;

    const graphics = scene.add.graphics();
    graphics.lineStyle(2, 0xffd700, 0.8);
    const line = new Phaser.Geom.Line(from.x, from.y, to.x, to.y);
    graphics.strokeLineShape(line);

    const dot = scene.add.circle(from.x, from.y, 5, 0xffffff);
    scene.tweens.add({
        targets: dot,
        x: to.x,
        y: to.y,
        duration: 1500,
        onComplete: () => {
            dot.destroy();
            graphics.destroy();
        }
    });

    const midX = (from.x + to.x) / 2;
    const midY = (from.y + to.y) / 2;
    const label = scene.add.text(midX, midY, resource, {
        fontSize: '14px',
        color: '#ffd700',
        stroke: '#000',
        strokeThickness: 3
    }).setOrigin(0.5);
    scene.tweens.add({
        targets: label,
        alpha: 0,
        duration: 2000,
        delay: 500,
        onComplete: () => label.destroy()
    });
};

// War explosion + camera shake
window.showWarExplosion = function (scene, x, y) {
    if (!scene) return;
    scene.cameras.main.shake(400, 0.04);
    const circle = scene.add.circle(x, y, 10, 0xff0000);
    scene.tweens.add({
        targets: circle,
        scaleX: 3,
        scaleY: 3,
        alpha: 0,
        duration: 600,
        onComplete: () => circle.destroy()
    });
};