#include <Mouse.h>
#include <usbhub.h>
#include <hidboot.h>

USB Usb;
HIDBoot<USB_HID_PROTOCOL_MOUSE> HidMouse(&Usb);

int8_t dx = 0, dy = 0, scroll = 0;
bool lmb = false, rmb = false, mmb = false, btn1 = false, btn2 = false;

class MouseRptParser : public MouseReportParser {
protected:
  void Parse(USBHID *hid, bool is_rpt_id, uint8_t len, uint8_t *buf) override {
    if (len < 4) return;

    dx = static_cast<int8_t>(buf[1]);
    dy = static_cast<int8_t>(buf[2]);
    scroll = static_cast<int8_t>(buf[3]);

    if (scroll != 0) Mouse.move(0, 0, scroll);

    uint8_t buttons = buf[0];

    if (buttons & 0x01) {
      if (!lmb) {
        lmb = true;
        Mouse.press(MOUSE_LEFT);
      }
    } else if (lmb) {
      lmb = false;
      Mouse.release(MOUSE_LEFT);
    }

    if (buttons & 0x02) {
      if (!rmb) {
        rmb = true;
        Mouse.press(MOUSE_RIGHT);
      }
    } else if (rmb) {
      rmb = false;
      Mouse.release(MOUSE_RIGHT);
    }

    if (buttons & 0x04) {
      if (!mmb) {
        mmb = true;
        Mouse.press(MOUSE_MIDDLE);
      }
    } else if (mmb) {
      mmb = false;
      Mouse.release(MOUSE_MIDDLE);
    }

    if (buttons & 0x08) {
      if (!btn1) {
        btn1 = true;
        Mouse.press(MOUSE_BACKWARD);
      }
    } else if (btn1) {
      btn1 = false;
      Mouse.release(MOUSE_BACKWARD);
    }

    if (buttons & 0x10) {
      if (!btn2) {
        btn2 = true;
        Mouse.press(MOUSE_FORWARD);
      }
    } else if (btn2) {
      btn2 = false;
      Mouse.release(MOUSE_FORWARD);
    }
  }
};

MouseRptParser Prs;

void setup() {
  Serial.begin(9600);
  Mouse.begin();

  if (Usb.Init() == -1) {
    while (1);
  }
  HidMouse.SetReportParser(0, &Prs);
}

void loop() {
  Usb.Task();

  if (dx != 0 || dy != 0) {
    Mouse.move(dx, dy);
    dx = 0;
    dy = 0;
  }

  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    processCommand(command);
  }
}

void processCommand(String command) {
  if (command.startsWith("MOVE")) {
    int commaIndex = command.indexOf(',');
    if (commaIndex != -1) {
      int x = command.substring(4, commaIndex).toInt();
      int y = command.substring(commaIndex + 1).toInt();
      Mouse.move(x, y);
    }
  } else if (command.startsWith("FIRE")) {
    Mouse.press(MOUSE_LEFT);
    Mouse.release(MOUSE_LEFT);
  }
}
