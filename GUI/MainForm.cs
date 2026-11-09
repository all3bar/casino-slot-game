using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using CasinoSlot.Core;
using CasinoSlot.Accounting;
using CasinoSlot.Hardware;
using CasinoSlot.SAS;

namespace CasinoSlot.GUI
{
    public class MainForm : Form
    {
        private readonly GameManager _game;
        private readonly BillAcceptor _billAcceptor;
        private readonly PulseController _pulseController;
        private readonly System.Windows.Forms.Timer _spinTimer;
        private int _spinAnimationTicks = 0;

        // SAS 6.0 RS-232 Protocol
        private readonly MachineIdentity _machineIdentity;
        private readonly SASCommandProcessor _sasProcessor;
        private readonly SerialCommunication _sasSerial;
        private readonly List<byte> _rxBuffer = new List<byte>();
        private readonly object _rxLock = new object();
        private System.Threading.Timer _gpTimer;
        private bool _isDoorOpen = false;

        // Reel Grid UI: 5 reels x 3 rows in TableLayoutPanel
        private TableLayoutPanel _tlpReels;
        private readonly Label[,] _reelLabels = new Label[5, 3];
        private readonly Panel[,] _reelPanels = new Panel[5, 3];

        // Status & Displays
        private Label _lblStatus;
        private Label _lblCredits;
        private Label _lblTotalBet;
        private Label _lblWin;
        private Label _lblJackpot;
        private Label _lblState;
        private Label _lblComStatus;

        // Player Controls
        private Button _btnSpin;
        private Button _btnMaxBet;
        private Button _btnCashout;
        private Button _btnBetMinus;
        private Button _btnBetPlus;
        private Button _btnLinesMinus;
        private Button _btnLinesPlus;
        private Label _lblBetPerLine;
        private Label _lblActiveLines;

        // SAS & Hardware Controls
        private ComboBox _cbComPort;
        private Button _btnComToggle;
        private CheckBox _chkDoor;

        // Meters & Logs Display
        private ListView _lvMeters;
        private ListBox _lbEventLog;

        public MainForm()
        {
            GameManager.ResetInstance();
            _game = GameManager.Instance;
            _billAcceptor = new BillAcceptor(new[] { 100, 500, 1000, 2000, 5000, 10000 });
            _pulseController = new PulseController(coinsPerPulseIn: 1, creditsPerCoin: 1);

            // SAS 6.0 setup (Address = 1, linked to IGT SAS Host)
            _machineIdentity = new MachineIdentity
            {
                SASAddress = 0x01,
                SerialNumber = "SN000001",
                AssetNumber = "001",
                GameId = "CAS001"
            };
            _sasProcessor = new SASCommandProcessor(_machineIdentity, _game.Meters, _game, _game.EventLogger);
            _sasSerial = new SerialCommunication();
            _sasSerial.DataReceived += OnSerialDataReceived;
            _sasSerial.ErrorOccurred += err => SafeLogEvent($"⚠️ SAS Serial: {err}");

            // Initial Power-up exception for SAS Host
            _sasProcessor.ExceptionManager.QueueException(SASExceptionCodes.ACPowerRestored);

            // BillAcceptor wiring
            _billAcceptor.BillAccepted += (s, e) =>
            {
                _game.SlotMachine.AddCredits(e.DenominationCents);
                _game.Meters.IncrementBillAccepted(e.DenominationCents);
                _sasProcessor.ExceptionManager.QueueException(SASExceptionCodes.BillAccepted);
                LogEvent($"💵 Bill Accepted: ${(e.DenominationCents / 100.0):F2} (+{e.DenominationCents} credits)");
                UpdateDisplays();
            };

            // Pulse wiring
            _pulseController.CoinAccepted += credits =>
            {
                _game.SlotMachine.AddCredits(credits);
                LogEvent($"🪙 Coin Accepted: +{credits} credit(s)");
                UpdateDisplays();
            };

            _billAcceptor.Enable();

            // Spin animation timer
            _spinTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _spinTimer.Tick += OnSpinTimerTick;

            InitializeComponent();
            UpdateReelDisplay(_game.SlotMachine.BuildGrid());
            UpdateDisplays();

            // Initial test credits
            if (_game.SlotMachine.Credits == 0)
            {
                _game.SlotMachine.AddCredits(1000);
                LogEvent("🎮 Initial test credits loaded: 1,000");
                UpdateDisplays();
            }
        }

        private void InitializeComponent()
        {
            Text = "🎰 Casino Slot Game - SAS 6.0 & Hardware GUI Simulator";
            Size = new Size(1200, 780);
            MinimumSize = new Size(950, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 20, 26);
            ForeColor = Color.White;
            KeyPreview = true;
            KeyDown += OnFormKeyDown;
            Resize += OnFormResized;

            // 1. Header (Responsive TableLayoutPanel: Title 30%, State 18%, COM Status 28%, Jackpot 24%)
            var tlpHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Color.FromArgb(26, 30, 40),
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(12, 0, 12, 0)
            };
            tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22f));

            var lblTitle = new Label
            {
                Text = "🎰 PRO SLOT 5000",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.Gold,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblState = new Label
            {
                Text = "STATE: IDLE",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.LightGreen,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblComStatus = new Label
            {
                Text = "🔌 포트: 연결 중...",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblJackpot = new Label
            {
                Text = "💰 JACKPOT: 1,000",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            tlpHeader.Controls.Add(lblTitle, 0, 0);
            tlpHeader.Controls.Add(_lblState, 1, 0);
            tlpHeader.Controls.Add(_lblComStatus, 2, 0);
            tlpHeader.Controls.Add(_lblJackpot, 3, 0);

            // 2. Main Body TableLayoutPanel (Left: 42% Slot Machine, Right: 58% Hardware & SAS)
            var tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(18, 20, 26),
                Padding = new Padding(2)
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var pnlLeft = new Panel { Dock = DockStyle.Fill };
            var pnlRight = new Panel { Dock = DockStyle.Fill };

            BuildResponsiveGameArea(pnlLeft);
            BuildResponsiveRightPanel(pnlRight);

            tlpMain.Controls.Add(pnlLeft, 0, 0);
            tlpMain.Controls.Add(pnlRight, 1, 0);

            Controls.Add(tlpMain);
            Controls.Add(tlpHeader);

            Load += (s, e) =>
            {
                AdjustReelFonts();
                ConnectSasPort(_cbComPort.SelectedItem?.ToString() ?? "COM4");
            };
        }

        private void BuildResponsiveGameArea(Panel parent)
        {
            parent.BackColor = Color.FromArgb(16, 18, 23);
            parent.Padding = new Padding(6);

            var tlpLeft = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.FromArgb(16, 18, 23),
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Row 0: Reels 5x3 Grid
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // Row 1: Status Bar
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 226f)); // Row 2: Player Controls

            // Row 0: Reels 5x3 TableLayoutPanel
            _tlpReels = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 3,
                BackColor = Color.FromArgb(12, 14, 18),
                Padding = new Padding(2),
                Margin = new Padding(0)
            };

            for (int c = 0; c < 5; c++)
                _tlpReels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

            for (int r = 0; r < 3; r++)
                _tlpReels.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

            for (int c = 0; c < 5; c++)
            {
                for (int r = 0; r < 3; r++)
                {
                    var pnlCell = new Panel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(26, 30, 42),
                        Margin = new Padding(2),
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    var lbl = new Label
                    {
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                        ForeColor = Color.White
                    };

                    pnlCell.Controls.Add(lbl);
                    _tlpReels.Controls.Add(pnlCell, c, r);

                    _reelPanels[c, r] = pnlCell;
                    _reelLabels[c, r] = lbl;
                }
            }
            tlpLeft.Controls.Add(_tlpReels, 0, 0);

            // Row 1: Status Message (Spacious with top/bottom separation)
            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.Yellow,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(24, 28, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Press [SPIN] or [Spacebar] to start",
                Margin = new Padding(2, 4, 2, 6)
            };
            tlpLeft.Controls.Add(_lblStatus, 0, 1);

            // Row 2: Player Controls TableLayoutPanel
            var tlpControls = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.FromArgb(20, 24, 32),
                Margin = new Padding(0),
                Padding = new Padding(6, 4, 6, 6)
            };
            tlpControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f)); // Row 0: Displays (Credits, Total Bet, Last Win)
            tlpControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f)); // Row 1: Bet Adjust
            tlpControls.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Row 2: Action Buttons

            BuildPlayerControlRows(tlpControls);
            tlpLeft.Controls.Add(tlpControls, 0, 2);

            parent.Controls.Add(tlpLeft);
        }

        private void BuildPlayerControlRows(TableLayoutPanel parent)
        {
            // Row 0: Displays (Credits, Total Bet, Last Win)
            var tlpDisplays = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 4)
            };
            tlpDisplays.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpDisplays.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tlpDisplays.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            tlpDisplays.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _lblCredits = CreateDisplayCell(tlpDisplays, "CREDITS", "1,000", Color.Cyan, 0);
            _lblTotalBet = CreateDisplayCell(tlpDisplays, "TOTAL BET", "20", Color.Orange, 1);
            _lblWin = CreateDisplayCell(tlpDisplays, "LAST WIN", "0", Color.LightGreen, 2);
            parent.Controls.Add(tlpDisplays, 0, 0);

            // Row 1: Bet Adjustments (2-Column Balanced Layout)
            var tlpAdjust = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 4),
                Padding = new Padding(0),
                BackColor = Color.FromArgb(24, 28, 38)
            };
            tlpAdjust.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpAdjust.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpAdjust.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Line Bet Stepper
            var pnlBet = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(2),
                Padding = new Padding(4, 8, 4, 4)
            };
            var lblBetTag = new Label
            {
                Text = "Line Bet:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Gainsboro,
                Margin = new Padding(2, 6, 4, 0)
            };
            _btnBetMinus = CreateAdjustButton("▼", (s, e) => ChangeBet(-1));
            _lblBetPerLine = new Label
            {
                Text = "1",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Size = new Size(36, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(14, 16, 22),
                ForeColor = Color.Gold,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(2, 1, 2, 0)
            };
            _btnBetPlus = CreateAdjustButton("▲", (s, e) => ChangeBet(+1));
            pnlBet.Controls.AddRange(new Control[] { lblBetTag, _btnBetMinus, _lblBetPerLine, _btnBetPlus });

            // Lines Stepper
            var pnlLines = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(2),
                Padding = new Padding(4, 8, 4, 4)
            };
            var lblLinesTag = new Label
            {
                Text = "Lines:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Gainsboro,
                Margin = new Padding(4, 6, 4, 0)
            };
            _btnLinesMinus = CreateAdjustButton("▼", (s, e) => ChangeLines(-1));
            _lblActiveLines = new Label
            {
                Text = "20",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Size = new Size(36, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(14, 16, 22),
                ForeColor = Color.LightGreen,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(2, 1, 2, 0)
            };
            _btnLinesPlus = CreateAdjustButton("▲", (s, e) => ChangeLines(+1));
            pnlLines.Controls.AddRange(new Control[] { lblLinesTag, _btnLinesMinus, _lblActiveLines, _btnLinesPlus });

            tlpAdjust.Controls.Add(pnlBet, 0, 0);
            tlpAdjust.Controls.Add(pnlLines, 1, 0);
            parent.Controls.Add(tlpAdjust, 0, 1);

            // Row 2: Action Buttons (Cashout, Max Bet, Spin)
            var tlpButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 2, 0, 2)
            };
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27f));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27f));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
            tlpButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _btnCashout = new Button
            {
                Text = "CASHOUT (C)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 3, 3, 4),
                BackColor = Color.FromArgb(150, 42, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCashout.FlatAppearance.BorderSize = 1;
            _btnCashout.FlatAppearance.BorderColor = Color.FromArgb(200, 80, 80);
            _btnCashout.Click += (s, e) => OnCashoutClicked();

            _btnMaxBet = new Button
            {
                Text = "MAX BET (M)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 3, 3, 4),
                BackColor = Color.FromArgb(185, 110, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnMaxBet.FlatAppearance.BorderSize = 1;
            _btnMaxBet.FlatAppearance.BorderColor = Color.FromArgb(235, 150, 50);
            _btnMaxBet.Click += (s, e) => OnMaxBetClicked();

            _btnSpin = new Button
            {
                Text = "🎰 SPIN (Space)",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 3, 2, 4),
                BackColor = Color.FromArgb(32, 145, 62),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnSpin.FlatAppearance.BorderSize = 1;
            _btnSpin.FlatAppearance.BorderColor = Color.FromArgb(60, 200, 100);
            _btnSpin.Click += (s, e) => ExecuteSpinAction();

            tlpButtons.Controls.Add(_btnCashout, 0, 0);
            tlpButtons.Controls.Add(_btnMaxBet, 1, 0);
            tlpButtons.Controls.Add(_btnSpin, 2, 0);
            parent.Controls.Add(tlpButtons, 0, 2);
        }

        private Label CreateDisplayCell(TableLayoutPanel parent, string title, string initialValue, Color valColor, int col)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                BackColor = Color.FromArgb(28, 32, 44),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(2)
            };

            var lblTag = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 185, 210),
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblVal = new Label
            {
                Text = initialValue,
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = valColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.Add(lblVal);
            pnl.Controls.Add(lblTag);
            parent.Controls.Add(pnl, col, 0);

            return lblVal;
        }

        private Button CreateAdjustButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Size = new Size(30, 28),
                BackColor = Color.FromArgb(45, 52, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(1, 1, 1, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(70, 80, 105);
            btn.Click += onClick;
            return btn;
        }

        private void BuildResponsiveRightPanel(Panel parent)
        {
            parent.BackColor = Color.FromArgb(20, 22, 28);
            parent.Padding = new Padding(8);

            // 1. SAS RS-232 COM Port Control Box
            var pnlSasBox = new GroupBox
            {
                Text = "🔌 IGT SAS Host 직렬 통신 (RS-232)",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.Gold,
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(6)
            };

            var lblPort = new Label
            {
                Text = "포트:",
                Location = new Point(14, 32),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White
            };
            _cbComPort = new ComboBox
            {
                Location = new Point(66, 28),
                Size = new Size(115, 34),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 34, 46),
                ForeColor = Color.White
            };
            var availablePorts = new List<string>(System.IO.Ports.SerialPort.GetPortNames());
            foreach (var defaultPort in new[] { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10" })
            {
                if (!availablePorts.Contains(defaultPort))
                    availablePorts.Add(defaultPort);
            }
            _cbComPort.Items.AddRange(availablePorts.ToArray());
            _cbComPort.SelectedItem = availablePorts.Contains("COM4") ? "COM4" : (availablePorts.Count > 0 ? availablePorts[0] : "COM4");
            _cbComPort.SelectedIndexChanged += (s, e) =>
            {
                if (_sasSerial.IsOpen)
                    ConnectSasPort(_cbComPort.SelectedItem?.ToString() ?? "COM4");
            };

            _btnComToggle = new Button
            {
                Text = "연결 끊기",
                Location = new Point(190, 26),
                Size = new Size(120, 36),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(140, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnComToggle.Click += (s, e) => ToggleSasPort();

            _chkDoor = new CheckBox
            {
                Text = "🚪 슬롯 도어 열림 (0x11 예외 전송)",
                Location = new Point(325, 29),
                Size = new Size(350, 30),
                ForeColor = Color.FromArgb(255, 180, 100),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            _chkDoor.CheckedChanged += OnDoorCheckboxChanged;

            pnlSasBox.Controls.AddRange(new Control[] { lblPort, _cbComPort, _btnComToggle, _chkDoor });

            // 2. Hardware Mock Area
            var lblHwTitle = new Label
            {
                Text = "🛠 가상 지폐 / 코인 투입 (Mock)",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.Gold,
                Dock = DockStyle.Top,
                Height = 32
            };

            var pnlBillButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(2),
                WrapContents = false
            };

            int[] billDenoms = { 100, 500, 1000, 2000, 5000, 10000 };
            foreach (int cents in billDenoms)
            {
                int val = cents;
                var btn = new Button
                {
                    Text = $"${val / 100}",
                    Size = new Size(82, 38),
                    Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(38, 55, 80),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(3, 2, 4, 2)
                };
                btn.Click += (s, e) =>
                {
                    _billAcceptor.ProcessEscrow(val);
                    _billAcceptor.NotifyStacked(val);
                };
                pnlBillButtons.Controls.Add(btn);
            }

            var btnPulseCoin = new Button
            {
                Text = "🪙 코인 펄스 +1",
                Size = new Size(165, 38),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(45, 75, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3, 2, 4, 2)
            };
            btnPulseCoin.Click += (s, e) => _pulseController.OnCoinInPulse();
            pnlBillButtons.Controls.Add(btnPulseCoin);

            // 3. Meters Area
            var lblMeterTitle = new Label
            {
                Text = "📊 SAS 6.0 회계 계측기 (Meters)",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.Gold,
                Dock = DockStyle.Top,
                Height = 32
            };

            _lvMeters = new ListView
            {
                Dock = DockStyle.Top,
                Height = 170,
                View = View.Details,
                FullRowSelect = true,
                BackColor = Color.FromArgb(14, 16, 20),
                ForeColor = Color.LightCyan,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold)
            };
            _lvMeters.Columns.Add("계측 항목 (Meter)", 260);
            _lvMeters.Columns.Add("값 (Value)", 220);

            // 4. Event Logs Area
            var lblLogTitle = new Label
            {
                Text = "📜 SAS 통신 및 시스템 로그",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.Gold,
                Dock = DockStyle.Top,
                Height = 32
            };

            _lbEventLog = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 12, 16),
                ForeColor = Color.FromArgb(170, 210, 190),
                Font = new Font("Consolas", 11.5f),
                IntegralHeight = false
            };

            parent.Controls.Add(_lbEventLog);
            parent.Controls.Add(lblLogTitle);
            parent.Controls.Add(_lvMeters);
            parent.Controls.Add(lblMeterTitle);
            parent.Controls.Add(pnlBillButtons);
            parent.Controls.Add(lblHwTitle);
            parent.Controls.Add(pnlSasBox);
        }

        // =========================================================================
        // SAS 6.0 RS-232 Serial Port Communication Engine
        // =========================================================================

        private void ToggleSasPort()
        {
            if (_sasSerial.IsOpen)
            {
                DisconnectSasPort();
            }
            else
            {
                string portName = _cbComPort.SelectedItem?.ToString() ?? "COM4";
                ConnectSasPort(portName);
            }
        }

        private void ConnectSasPort(string portName)
        {
            try
            {
                if (_sasSerial.IsOpen)
                    _sasSerial.Close();

                bool success = _sasSerial.Open(portName, 19200);
                if (success)
                {
                    _lblComStatus.Text = $"🔌 {portName}: 연결됨 🟢";
                    _lblComStatus.ForeColor = Color.LightGreen;
                    _btnComToggle.Text = "연결 끊기";
                    _btnComToggle.BackColor = Color.FromArgb(140, 40, 40);
                    LogEvent($"✅ SAS 직렬 포트 열림: {portName} (19200 Baud, 8N1) - IGT Host 통신 대기");
                }
                else
                {
                    _lblComStatus.Text = $"🔌 {portName}: 실패 🔴";
                    _lblComStatus.ForeColor = Color.Salmon;
                    _btnComToggle.Text = "연결";
                    _btnComToggle.BackColor = Color.FromArgb(40, 100, 60);
                    LogEvent($"❌ SAS 직렬 포트({portName}) 열기 실패 (다른 프로그램 사용 중 확인)");
                }
            }
            catch (Exception ex)
            {
                _lblComStatus.Text = $"🔌 {portName}: 오류 🔴";
                _lblComStatus.ForeColor = Color.Salmon;
                _btnComToggle.Text = "연결";
                _btnComToggle.BackColor = Color.FromArgb(40, 100, 60);
                LogEvent($"❌ {portName} 오류: {ex.Message}");
            }
        }

        private void DisconnectSasPort()
        {
            try
            {
                string portName = _sasSerial.PortName ?? _cbComPort.SelectedItem?.ToString() ?? "COM4";
                _gpTimer?.Dispose();
                _gpTimer = null;
                _rxBuffer.Clear();

                _sasSerial.Close();

                _lblComStatus.Text = $"🔌 {portName}: 연결 끊김 ⚪";
                _lblComStatus.ForeColor = Color.DarkGray;
                _btnComToggle.Text = "연결";
                _btnComToggle.BackColor = Color.FromArgb(40, 100, 60);
                LogEvent($"🔌 SAS 직렬 포트({portName}) 연결 끊김 (닫힘)");
            }
            catch (Exception ex)
            {
                LogEvent($"⚠️ 포트 닫기 오류: {ex.Message}");
            }
        }

        private void OnSerialDataReceived(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            lock (_rxLock)
            {
                _rxBuffer.AddRange(data);
                ProcessRxBuffer();
            }
        }

        private void ProcessRxBuffer()
        {
            // Process incoming SAS bytes in buffer
            while (_rxBuffer.Count > 0)
            {
                byte first = _rxBuffer[0];

                // 1. If high bit is set (>= 0x80), this is a 1-byte General Poll or Global Sync Poll
                if ((first & 0x80) != 0)
                {
                    _rxBuffer.RemoveAt(0);

                    byte addr = (byte)(first & 0x7F);
                    // If addressed to this machine, reply immediately (within SAS 45ms window)
                    if (addr == _machineIdentity.SASAddress)
                    {
                        byte[] resp = _sasProcessor.HandleGeneralPoll(first);
                        if (resp != null && resp.Length > 0)
                        {
                            _sasSerial.Send(resp);
                            if (resp[0] != 0x00)
                            {
                                SafeLogEvent($"📡 SAS GP [0x{first:X2}] ➜ 예외 전송: 0x{resp[0]:X2}");
                            }
                        }
                    }
                    continue;
                }

                // 2. If first byte is NOT addressed to this machine, discard it
                if (first != _machineIdentity.SASAddress && first != SASConstants.BroadcastAddress)
                {
                    _rxBuffer.RemoveAt(0);
                    continue;
                }

                // 3. Candidate for Long Poll (minimum length is 4 bytes: Addr + Cmd + CRC16)
                bool packetFound = false;
                for (int len = 4; len <= _rxBuffer.Count; len++)
                {
                    byte[] candidate = new byte[len];
                    _rxBuffer.CopyTo(0, candidate, 0, len);
                    if (SASProtocol.ValidateCRC(candidate))
                    {
                        _rxBuffer.RemoveRange(0, len);
                        HandleIncomingLongPoll(candidate);
                        packetFound = true;
                        break;
                    }
                }

                if (packetFound)
                {
                    continue;
                }

                // Prevent buffer overflow if corrupted data is accumulating
                if (_rxBuffer.Count > 64)
                {
                    _rxBuffer.RemoveAt(0);
                    continue;
                }

                // Incomplete packet: wait for more bytes
                break;
            }
        }

        private void HandleIncomingLongPoll(byte[] packet)
        {
            byte cmd = packet.Length > 1 ? packet[1] : (byte)0;
            string rxHex = BitConverter.ToString(packet).Replace("-", " ");
            SafeLogEvent($"📥 SAS RX LP [0x{cmd:X2}]: {rxHex}");

            byte[] response = _sasProcessor.ProcessMessage(packet);
            if (response != null && response.Length > 0)
            {
                _sasSerial.Send(response);
                string txHex = BitConverter.ToString(response).Replace("-", " ");
                SafeLogEvent($"📤 SAS TX LP [0x{cmd:X2}]: {txHex}");
                BeginInvoke(() => UpdateDisplays());
            }
        }

        private void OnDoorCheckboxChanged(object sender, EventArgs e)
        {
            _isDoorOpen = _chkDoor.Checked;
            if (_isDoorOpen)
            {
                _game.EnterMaintenance();
                _sasProcessor.ExceptionManager.QueueException(SASExceptionCodes.SlotDoorOpened);
                LogEvent("🚪 슬롯 도어 열림 ➜ Exception 0x11 대기열 추가 (Host 다음 GP에 보고)");
            }
            else
            {
                _game.ExitMaintenance();
                _sasProcessor.ExceptionManager.QueueException(SASExceptionCodes.SlotDoorClosed);
                LogEvent("🚪 슬롯 도어 닫힘 ➜ Exception 0x12 대기열 추가 (Host 다음 GP에 보고)");
            }
            UpdateDisplays();
        }

        private void SafeLogEvent(string msg)
        {
            if (IsDisposed || Disposing) return;
            try
            {
                if (InvokeRequired)
                    BeginInvoke(() => LogEvent(msg));
                else
                    LogEvent(msg);
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _gpTimer?.Dispose();
            _sasSerial?.Close();
            base.OnFormClosing(e);
        }

        // =========================================================================
        // Dynamic Font & Responsive Scaling
        // =========================================================================

        private void OnFormResized(object sender, EventArgs e)
        {
            AdjustReelFonts();

            if (_lvMeters != null && _lvMeters.Columns.Count >= 2)
            {
                int totalWidth = Math.Max(200, _lvMeters.ClientSize.Width);
                _lvMeters.Columns[0].Width = (int)(totalWidth * 0.50);
                _lvMeters.Columns[1].Width = totalWidth - _lvMeters.Columns[0].Width - 4;
            }
        }

        private void AdjustReelFonts()
        {
            if (_tlpReels == null || _tlpReels.Height <= 0 || _tlpReels.Width <= 0) return;

            int cellHeight = _tlpReels.Height / 3;
            int cellWidth = _tlpReels.Width / 5;
            float targetSize = Math.Clamp(Math.Min(cellHeight / 4.0f, cellWidth / 4.4f), 9.5f, 15f);

            var newFont = new Font("Segoe UI", targetSize, FontStyle.Bold);

            for (int c = 0; c < 5; c++)
            {
                for (int r = 0; r < 3; r++)
                {
                    if (_reelLabels[c, r] != null)
                        _reelLabels[c, r].Font = newFont;
                }
            }
        }

        // =========================================================================
        // Game Logic & Events
        // =========================================================================

        private void ExecuteSpinAction()
        {
            if (_spinTimer.Enabled) return;

            if (_game.SlotMachine.Credits < _game.SlotMachine.TotalBet)
            {
                MessageBox.Show("크레딧이 부족합니다. 지폐 투입기 버튼으로 크레딧을 추가하세요!", "크레딧 부족",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _btnSpin.Enabled = false;
            _lblState.Text = "STATE: SPINNING";
            _lblState.ForeColor = Color.Orange;
            _lblStatus.Text = "Spinning reels...";

            // SAS Exception: Game Started (0x51)
            _sasProcessor.ExceptionManager.QueueException(SASExceptionCodes.GameStarted);

            _spinAnimationTicks = 0;
            _spinTimer.Start();
        }

        private void OnSpinTimerTick(object sender, EventArgs e)
        {
            _spinAnimationTicks++;

            var rand = new Random();
            for (int r = 0; r < 5; r++)
            {
                for (int row = 0; row < 3; row++)
                {
                    var dummySymbol = (SymbolType)rand.Next(0, 12);
                    SetCellSymbol(r, row, dummySymbol);
                }
            }

            if (_spinAnimationTicks >= 8)
            {
                _spinTimer.Stop();

                var result = _game.ExecuteSpin();
                if (result != null)
                {
                    UpdateReelDisplay(result.Grid);

                    // SAS Exception: Game Ended (0x52)
                    _sasProcessor.ExceptionManager.QueueException(SASExceptionCodes.GameEnded);

                    if (result.TotalWin > 0)
                    {
                        _lblStatus.Text = $"🎉 WIN! {result.TotalWin} CREDITS ({result.WinningLines.Count} paylines)";
                        _lblStatus.ForeColor = Color.Gold;
                        LogEvent($"🏆 Spin Win: +{result.TotalWin} credits! (Lines: {result.WinningLines.Count})");
                    }
                    else
                    {
                        _lblStatus.Text = "No Win. Try again!";
                        _lblStatus.ForeColor = Color.LightGray;
                        LogEvent($"🎲 Spin: Bet {_game.SlotMachine.TotalBet}, Win: 0");
                    }

                    _game.AcknowledgeResult();
                }

                _btnSpin.Enabled = true;
                _lblState.Text = "STATE: IDLE";
                _lblState.ForeColor = Color.LightGreen;
                UpdateDisplays();
            }
        }

        private void ChangeBet(int delta)
        {
            long current = _game.SlotMachine.BetPerLine;
            long next = Math.Clamp(current + delta, 1, 10);
            _game.SlotMachine.SetBetPerLine(next);
            UpdateDisplays();
        }

        private void ChangeLines(int delta)
        {
            int current = _game.SlotMachine.ActiveLinesCount;
            int next = Math.Clamp(current + delta, 1, 20);
            _game.SlotMachine.SetActiveLines(next);
            UpdateDisplays();
        }

        private void OnMaxBetClicked()
        {
            _game.SlotMachine.MaxBet();
            LogEvent("⚡ Max Bet applied (10 per line, 20 lines = 200 credits)");
            UpdateDisplays();
            ExecuteSpinAction();
        }

        private void OnCashoutClicked()
        {
            long credits = _game.Cashout();
            if (credits > 0)
            {
                // SAS Exception: Cashout Button Pressed (0x23)
                _sasProcessor.ExceptionManager.QueueException(SASExceptionCodes.CashoutButtonPressed);
                LogEvent($"💰 Cashout Completed: {credits} credits paid out via hopper.");
                MessageBox.Show($"총 {credits} 크레딧이 정산되었습니다!", "캐시아웃",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            UpdateDisplays();
        }

        private void UpdateReelDisplay(SymbolType[,] grid)
        {
            if (grid == null) return;

            for (int r = 0; r < 5; r++)
            {
                for (int row = 0; row < 3; row++)
                {
                    SetCellSymbol(r, row, grid[r, row]);
                }
            }
        }

        private void SetCellSymbol(int reel, int row, SymbolType type)
        {
            var (icon, color) = GetSymbolDisplay(type);
            var lbl = _reelLabels[reel, row];
            lbl.Text = icon;
            lbl.ForeColor = color;
        }

        private (string text, Color color) GetSymbolDisplay(SymbolType type) => type switch
        {
            SymbolType.Wild    => ("⭐\nWILD", Color.Gold),
            SymbolType.Seven   => ("7️⃣\nSEVEN", Color.FromArgb(255, 80, 80)),
            SymbolType.Bell    => ("🔔\nBELL", Color.FromArgb(255, 215, 0)),
            SymbolType.BAR3    => ("⬛\nBAR 3", Color.FromArgb(100, 200, 255)),
            SymbolType.BAR2    => ("⬛\nBAR 2", Color.FromArgb(255, 160, 60)),
            SymbolType.BAR1    => ("⬛\nBAR 1", Color.FromArgb(200, 140, 255)),
            SymbolType.Cherry  => ("🍒\nCHERRY", Color.FromArgb(255, 60, 90)),
            SymbolType.Lemon   => ("🍋\nLEMON", Color.FromArgb(255, 255, 100)),
            SymbolType.Orange  => ("🍊\nORANGE", Color.FromArgb(255, 140, 30)),
            SymbolType.Plum    => ("🍇\nPLUM", Color.FromArgb(210, 100, 255)),
            SymbolType.Scatter => ("💎\nSCATTER", Color.Cyan),
            SymbolType.Bonus   => ("🎁\nBONUS", Color.LimeGreen),
            _                  => ("?", Color.White)
        };

        private void UpdateDisplays()
        {
            _lblCredits.Text = $"{_game.SlotMachine.Credits:N0}";
            _lblTotalBet.Text = $"{_game.SlotMachine.TotalBet:N0}";
            _lblBetPerLine.Text = $"{_game.SlotMachine.BetPerLine}";
            _lblActiveLines.Text = $"{_game.SlotMachine.ActiveLinesCount}";

            var lastResult = _game.LastSpinResult;
            if (lastResult != null)
                _lblWin.Text = $"{lastResult.TotalWin:N0}";

            _lvMeters.Items.Clear();
            AddMeterRow("Total Coin In", $"{_game.Meters.TotalCoinIn:N0}");
            AddMeterRow("Total Coin Out", $"{_game.Meters.TotalCoinOut:N0}");
            AddMeterRow("Total Drop", $"{_game.Meters.TotalDrop:N0}");
            AddMeterRow("Total Bills Value", $"${(_game.Meters.TotalBillsValue / 100.0):F2}");
            AddMeterRow("Bills Accepted", $"{_game.Meters.TotalBillsAccepted:N0}");
            AddMeterRow("Games Played", $"{_game.Meters.GamesPlayed:N0}");
            AddMeterRow("Games Won", $"{_game.Meters.GamesWon:N0}");
            AddMeterRow("Return to Player (RTP)", $"{_game.Meters.RTP:F2}%");
        }

        private void AddMeterRow(string name, string val)
        {
            var item = new ListViewItem(name);
            item.SubItems.Add(val);
            _lvMeters.Items.Add(item);
        }

        private void LogEvent(string msg)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            _lbEventLog.Items.Insert(0, $"[{time}] {msg}");
            if (_lbEventLog.Items.Count > 100)
                _lbEventLog.Items.RemoveAt(_lbEventLog.Items.Count - 1);
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                ExecuteSpinAction();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.M)
            {
                OnMaxBetClicked();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.C)
            {
                OnCashoutClicked();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                ChangeBet(+1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                ChangeBet(-1);
                e.Handled = true;
            }
        }
    }
}
