import ctypes
from ctypes import wintypes

WNDENUMPROC = ctypes.WINFUNCTYPE(wintypes.BOOL,
                                 wintypes.HWND,
                                 wintypes.LPARAM)
user32 = ctypes.windll.user32
user32.EnumWindows.argtypes = [
    WNDENUMPROC,
    wintypes.LPARAM]
user32.GetWindowTextLengthW.argtypes = [
    wintypes.HWND]
user32.GetWindowTextW.argtypes = [
    wintypes.HWND,
    wintypes.LPWSTR,
    ctypes.c_int]

def worker(hwnd, lParam):
    length = user32.GetWindowTextLengthW(hwnd) + 1
    buffer = ctypes.create_unicode_buffer(length)
    user32.GetWindowTextW(hwnd, buffer, length)
    print("Buff: ", repr(buffer.value))
    return True

cb_worker = WNDENUMPROC(worker)
if not user32.EnumWindows(cb_worker, 42):
    raise ctypes.WinError()