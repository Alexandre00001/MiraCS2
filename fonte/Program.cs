using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class CrosshairForm : Form
{
    private System.Windows.Forms.Timer _timer;
    private bool _isCrosshairVisible = false; // Controla a visibilidade da mira

    // Tornar a delegação pública para que o SetWindowsHookEx possa usá-la
    public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    // Importa as funções da API do Windows para a captura global de teclas
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(int idHook);

    [DllImport("user32.dll")]
    public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;
    private const int VK_C = 0x43; // Código da tecla C

    private int _hookId = 0;
    private HookProc _hookCallback;

    public CrosshairForm()
    {
        // Configurações iniciais do formulário
        this.FormBorderStyle = FormBorderStyle.None;
        this.TopMost = true;
        this.BackColor = Color.Magenta; // Cor que será ignorada e considerada "transparente"
        this.Width = Screen.PrimaryScreen.Bounds.Width;
        this.Height = Screen.PrimaryScreen.Bounds.Height;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(0, 0);
        this.TransparencyKey = Color.Magenta;

        // Desabilitar a interação com o mouse
        this.SetStyle(ControlStyles.Selectable, false);
        this.SetStyle(ControlStyles.Opaque, false);
        this.SetStyle(ControlStyles.UserMouse, false);

        // Configura o temporizador para redesenhar a mira
        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 10; // Redesenha a mira a cada 10ms
        _timer.Tick += (sender, e) => Invalidate();
        _timer.Start();

        // Registrar o hook global para capturar a tecla C
        _hookCallback = HookCallback;
        _hookId = SetHook(_hookCallback);

        if (_hookId == 0)
        {
            MessageBox.Show("Erro ao configurar o hook global. O aplicativo será fechado.");
            Application.Exit();
        }
    }

    // Define o hook global
    private int SetHook(HookProc hookProc)
    {
        IntPtr hInstance = GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName);
        return SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, hInstance, 0);
    }

    // Callback do hook global para capturar a tecla C
    private int HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0 && wParam == (IntPtr)0x0100) // Verifica se a tecla foi pressionada
            {
                int keyCode = Marshal.ReadInt32(lParam);
                if (keyCode == VK_C) // Se a tecla pressionada for 'C'
                {
                    _isCrosshairVisible = !_isCrosshairVisible; // Alterna a visibilidade da mira
                    Invalidate(); // Redesenha a tela
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro no callback do hook: {ex.Message}");
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    // Sobrescrevendo o método WndProc para garantir que o mouse não interaja com a janela
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTTRANSPARENT = -1;

        if (m.Msg == WM_NCHITTEST)
        {
            m.Result = (IntPtr)HTTRANSPARENT;
        }
        else
        {
            base.WndProc(ref m);
        }
    }

    // Método para desenhar a mira inspirada na configuração fornecida
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (!_isCrosshairVisible)
            return;

        int centerX = this.Width / 2;
        int centerY = this.Height / 2;

        // Parâmetros da mira ajustada
        int crosshairSize = 2; // Tamanho das linhas
        int lineSpacing = 1;    // Espaçamento do centro
        int lineThickness = 2;  // Espessura das linhas

        Pen crosshairPen = new Pen(Color.Black, lineThickness); // Linha preta

        // Linha horizontal esquerda
        e.Graphics.DrawLine(crosshairPen, centerX - lineSpacing - crosshairSize, centerY, centerX - lineSpacing, centerY);
        // Linha horizontal direita
        e.Graphics.DrawLine(crosshairPen, centerX + lineSpacing, centerY, centerX + lineSpacing + crosshairSize, centerY);
        // Linha vertical superior
        e.Graphics.DrawLine(crosshairPen, centerX, centerY - lineSpacing - crosshairSize, centerX, centerY - lineSpacing);
        // Linha vertical inferior
        e.Graphics.DrawLine(crosshairPen, centerX, centerY + lineSpacing, centerX, centerY + lineSpacing + crosshairSize);
    }

    // Método para remover o hook quando o aplicativo for fechado
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_hookId != 0)
        {
            UnhookWindowsHookEx(_hookId);
        }
        base.OnFormClosing(e);
    }

    [STAThread]
    public static void Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CrosshairForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro crítico na aplicação: {ex.Message}");
        }
    }
}
