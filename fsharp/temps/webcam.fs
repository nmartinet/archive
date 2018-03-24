open System
open System.IO
open System.Text
open System.Windows.Forms
open System.Drawing
open AForge.Video
open AForge.Video.DirectShow
open AForge.Imaging
open AForge.Imaging.Filters
open AForge
open System.Runtime.InteropServices
open System.Threading

open System
open System.Runtime.InteropServices

let p x = printfn "%A" x

module Win32 =
  type dwEnumDelegate = delegate of IntPtr * int -> bool

  [<DllImport("user32.dll", CharSet=CharSet.Auto)>]
  extern IntPtr SendMessage(IntPtr hWnd, uint32 Msg, IntPtr wParam , IntPtr lParam)
  [<DllImport("user32.dll", CharSet=CharSet.Auto)>]
  extern IntPtr EnumDesktopWindows(IntPtr hDesktop, dwEnumDelegate lpEnumCallbackFunction, IntPtr lParam)
  [<DllImport("user32.dll", CharSet=CharSet.Auto)>]
  extern IntPtr GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount)
  [<DllImport("avicap32.dll", CharSet=CharSet.Auto)>]
  extern IntPtr capCreateCaptureWindow(string lpszWindowName, int dwStyle, int X, int Y, int nWidth, int nHeight, int hwndParent, int nID )
  [<DllImport("avicap32.dll", CharSet=CharSet.Auto)>]
  extern bool capGetDriverDescription(IntPtr wDriver, string lpszName, int cbName, string lpszVer, int cbVer )


  let WM_CAP_CONNECT = 1034
  let WM_CAP_DISCONNECT = 1035
  let WM_CAP_COPY = 1054
  let WM_CAP_GET_FRAME = 1084


let CapWC () =
  Clipboard.Clear()

  

  let capWin = Win32.capCreateCaptureWindow("ccWebC", 0, 0, 0, 350, 350, 0, 0)
  Win32.SendMessage(capWin, (uint32)Win32.WM_CAP_CONNECT, IntPtr.Zero, IntPtr.Zero)
  p "sleep"
  Thread.Sleep(1500)
  p "sleep end"
  Win32.SendMessage(capWin, (uint32)Win32.WM_CAP_GET_FRAME, IntPtr.Zero, IntPtr.Zero)
  Win32.SendMessage(capWin, (uint32)Win32.WM_CAP_COPY, IntPtr.Zero, IntPtr.Zero)
  

  if (Clipboard.ContainsImage()) then
    let img =  Clipboard.GetImage()
    img.Save(@"C:\_home\tmp.jpeg", Imaging.ImageFormat.Jpeg)
  Win32.SendMessage(capWin, (uint32)Win32.WM_CAP_DISCONNECT, IntPtr.Zero, IntPtr.Zero)

let listWins () =
  let enumDelegate : Win32.dwEnumDelegate =
    Win32.dwEnumDelegate(fun (hWnd : IntPtr) (lParam : int) ->
      let sb = new StringBuilder(255)
      Win32.GetWindowText(hWnd, sb, sb.Capacity+1)
      printfn "%A" (sb.ToString())
      true
    )

  Win32.EnumDesktopWindows(IntPtr.Zero, enumDelegate , IntPtr.Zero)

    

[<EntryPoint; STAThread>]
let main argv = 
  CapWC()
  System.Console.ReadLine()
  0 // return an integer exit code
