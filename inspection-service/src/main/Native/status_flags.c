/*
 * status_flags.c
 *
 * Bitwise status flag parser for FactoryPulse equipment state.
 * Compiled as a shared library and called from the .NET inspection service
 * via P/Invoke. Handles the low-level flag decoding so the .NET side stays clean.
 *
 * Build:
 *   gcc -shared -fPIC -O2 -o libstatusflag.so status_flags.c
 *
 * Flag layout (32-bit int):
 *   Bits 0-3:  primary status  (0=Online, 1=Offline, 2=Maintenance, 3=Fault)
 *   Bit  4:    overheating alert
 *   Bit  5:    vibration alert
 *   Bit  6:    pressure alert
 *   Bit  7:    low lubricant
 *   Bits 8-15: last fault code (0 = none)
 *   Bits 16-31: reserved
 */

#include <stdint.h>
#include <string.h>

#define FLAG_STATUS_MASK    0x0F
#define FLAG_OVERHEATING    (1 << 4)
#define FLAG_VIBRATION      (1 << 5)
#define FLAG_PRESSURE       (1 << 6)
#define FLAG_LOW_LUBRICANT  (1 << 7)
#define FLAG_FAULT_CODE_SHIFT 8
#define FLAG_FAULT_CODE_MASK  0xFF

typedef struct {
    int   primary_status;    /* 0=Online, 1=Offline, 2=Maintenance, 3=Fault */
    int   overheating;
    int   vibration;
    int   pressure_alert;
    int   low_lubricant;
    int   fault_code;        /* 0 = no fault */
    int   alert_count;       /* total number of active alerts */
} StatusFlags;

/*
 * Parse a raw 32-bit status flags integer into a StatusFlags struct.
 * Returns 0 on success, -1 on null pointer.
 */
int parse_status_flags(uint32_t raw, StatusFlags *out)
{
    if (!out) return -1;

    memset(out, 0, sizeof(StatusFlags));

    out->primary_status  = (int)(raw & FLAG_STATUS_MASK);
    out->overheating     = (raw & FLAG_OVERHEATING)   ? 1 : 0;
    out->vibration       = (raw & FLAG_VIBRATION)     ? 1 : 0;
    out->pressure_alert  = (raw & FLAG_PRESSURE)      ? 1 : 0;
    out->low_lubricant   = (raw & FLAG_LOW_LUBRICANT) ? 1 : 0;
    out->fault_code      = (int)((raw >> FLAG_FAULT_CODE_SHIFT) & FLAG_FAULT_CODE_MASK);

    out->alert_count = out->overheating
                     + out->vibration
                     + out->pressure_alert
                     + out->low_lubricant;

    /* If any alert is active but status shows Online, upgrade to Fault */
    if (out->alert_count > 0 && out->primary_status == 0)
        out->primary_status = 3;

    return 0;
}

/*
 * Write the primary status string into buf (caller allocates, min 16 bytes).
 * Returns buf for convenience.
 */
const char *status_to_string(int primary_status, char *buf, int buf_len)
{
    const char *names[] = { "Online", "Offline", "Maintenance", "Fault" };
    const char *name = (primary_status >= 0 && primary_status <= 3)
                        ? names[primary_status]
                        : "Unknown";
    strncpy(buf, name, (size_t)(buf_len - 1));
    buf[buf_len - 1] = '\0';
    return buf;
}

/*
 * Encode a StatusFlags struct back to a raw uint32.
 * Useful when updating flags before writing back to DB.
 */
uint32_t encode_status_flags(const StatusFlags *flags)
{
    if (!flags) return 0;

    uint32_t raw = (uint32_t)(flags->primary_status & FLAG_STATUS_MASK);

    if (flags->overheating)    raw |= FLAG_OVERHEATING;
    if (flags->vibration)      raw |= FLAG_VIBRATION;
    if (flags->pressure_alert) raw |= FLAG_PRESSURE;
    if (flags->low_lubricant)  raw |= FLAG_LOW_LUBRICANT;

    raw |= (uint32_t)(flags->fault_code & 0xFF) << FLAG_FAULT_CODE_SHIFT;

    return raw;
}
