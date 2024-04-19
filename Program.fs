open System
open System.Threading
open Raylib_cs
open System.Runtime.InteropServices

type LowLevelKeyboardProc = delegate of nCode:int * wParam:nativeint * lParam:nativeint -> nativeint

[<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
extern nativeint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nativeint hMod, uint dwThreadId)

[<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
extern nativeint CallNextHookEx(nativeint hhk, int nCode, nativeint wParam, nativeint lParam)

[<DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
extern nativeint GetModuleHandle(string lpModuleName)

let WH_KEYBOARD_LL = 13
let WM_KEYDOWN = nativeint 0x0100
[<Struct>]
[<StructLayout(LayoutKind.Sequential)>]
type POINT = {
    X : int
    Y : int
}

[<Struct>]
[<StructLayout(LayoutKind.Sequential)>]
type MSG = {
    hwnd : nativeint
    message : uint
    wParam : unativeint
    lParam : nativeint
    time : int
    pt : POINT
    lPrivate : int
}

[<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
extern int GetMessage(MSG& lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax)

[<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
extern bool TranslateMessage([<In>] MSG& lpMsg)

[<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
extern IntPtr DispatchMessage([<In>] MSG& lpmsg)

[<Struct>]
[<StructLayout(LayoutKind.Sequential)>]
type KBDLLHOOKSTRUCT = {
  vkCode : int
  scanCode : int
  flags : int
  time : int
  dwExtraInfo : nativeint
}

Raylib.SetTraceLogLevel(TraceLogLevel.Error)
Raylib.InitAudioDevice()

let rnd = new Random()
let sounds =
    IO.Directory.EnumerateFiles("sounds")
    |> Seq.toArray

let ActiveSounds = System.Collections.Generic.List<Sound>()

let HookCallback (nCode:int) (wParam:nativeint) (lParam:nativeint) : IntPtr =
    try
        if nCode >= 0 && wParam = WM_KEYDOWN then
            ActiveSounds.RemoveAll(fun sound ->
                if CBool.op_Implicit(Raylib.IsSoundPlaying(sound)) then
                    false
                else
                    Raylib.UnloadSound(sound)
                    true
            ) |> ignore
            let data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam)
            let sound = Raylib.LoadSound(sounds[data.vkCode % sounds.Length])
            Raylib.SetSoundPitch(sound, float32 <| Math.Pow(1.2, rnd.NextDouble() - 0.5))
            Raylib.PlaySound(sound)
            ActiveSounds.Add(sound)
    with _ -> ()

    CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam)

let hookID =
    use curProcess = System.Diagnostics.Process.GetCurrentProcess()
    SetWindowsHookEx(WH_KEYBOARD_LL, HookCallback, GetModuleHandle(curProcess.MainModule.ModuleName), 0u)

Console.WriteLine("Ready to mine!")

let mutable Msg = Unchecked.defaultof<MSG >
while 0 < GetMessage(&Msg, IntPtr.Zero, 0u, 0u) do
    ignore <| TranslateMessage(&Msg)
    ignore <| DispatchMessage(&Msg)

Raylib.CloseAudioDevice()
