# Implementation Plan: FIFO Stock Cutting System

## ระบบตัด Stock วัตถุดิบแบบ FIFO สำหรับ 4 สิทธิประโยชน์

---

## 1. ภาพรวมระบบ (System Overview)

```
Import (นำเข้า)                    สูตรการผลิต (BOM)              Export (ส่งออก)
┌─────────────┐                  ┌──────────────┐              ┌─────────────┐
│ import_excel │                  │ bom_m29_hd/dt│              │ export_excel │
│ import_excel │                  │ bom_boi_hd/dt│              │ export_excel │
└──────┬──────┘                  └──────┬───────┘              └──────┬──────┘
       │                                │                             │
       ▼                                ▼                             ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                        Stock Card (FIFO Queue)                          │
│                                                                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐                │
│  │ Lot 1    │→ │ Lot 2    │→ │ Lot 3    │→ │ Lot 4    │  ← FIFO      │
│  │ เก่าสุด   │  │          │  │          │  │ ใหม่สุด   │                │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘                │
└──────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                     Stock Cutting Transaction                            │
│                                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐  │
│  │ 19 ทวิ       │  │ BOI          │  │ ชดเชย         │  │ โอนสิทธิ์    │  │
│  │ คืนอากร      │  │ ตัดบัญชี      │  │ เงินชดเชย     │  │ โอนข้ามบริษัท│  │
│  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Database Design (ตาราง)

### 2.1 imp.stock_card — Stock Card หลัก (บัญชีรับ-จ่ายวัตถุดิบ)

```sql
CREATE TABLE [imp].[stock_card] (
    [Id]                  INT IDENTITY(1,1) NOT NULL,
    [TransactionDate]     DATE              NOT NULL,       -- วันที่ทำรายการ
    [TransactionType]     NVARCHAR(20)      NOT NULL,       -- 'IN' = รับเข้า, 'OUT' = ตัดออก, 'TRANSFER_OUT' = โอนออก, 'TRANSFER_IN' = รับโอน
    [PrivilegeType]       NVARCHAR(20)      NOT NULL,       -- '19TVIS', 'BOI', 'COMPENSATION', 'TRANSFER'

    -- อ้างอิง Import (รับเข้า)
    [ImportDeclarNo]      NVARCHAR(50)      NULL,           -- เลขที่ใบขนขาเข้า
    [ImportItemNo]        INT               NULL,           -- ลำดับรายการในใบขน
    [ImportDate]          DATE              NULL,           -- วันนำเข้า (ใช้เรียง FIFO)

    -- อ้างอิง Export (ตัดออก)
    [ExportDeclarNo]      NVARCHAR(50)      NULL,           -- เลขที่ใบขนขาออก
    [ExportItemNo]        INT               NULL,           -- ลำดับรายการในใบขน

    -- วัตถุดิบ
    [RawMaterialCode]     NVARCHAR(50)      NOT NULL,       -- รหัสวัตถุดิบ (จาก BOM)
    [ProductCode]         NVARCHAR(50)      NULL,           -- รหัสสินค้า
    [ProductDescription]  NVARCHAR(500)     NULL,           -- ชื่อวัตถุดิบ
    [Unit]                NVARCHAR(20)      NOT NULL,       -- หน่วย (KG, TNE, etc.)

    -- ปริมาณ
    [QtyIn]               DECIMAL(18,6)     NULL,           -- ปริมาณรับเข้า
    [QtyOut]              DECIMAL(18,6)     NULL,           -- ปริมาณตัดออก
    [QtyBalance]          DECIMAL(18,6)     NOT NULL,       -- ยอดคงเหลือหลังทำรายการ

    -- มูลค่า/อากร
    [UnitPrice]           DECIMAL(18,4)     NULL,           -- ราคาต่อหน่วย
    [CIFValueTHB]         DECIMAL(18,4)     NULL,           -- มูลค่า CIF (บาท)
    [DutyRate]            DECIMAL(18,4)     NULL,           -- อัตราอากร (%)
    [DutyAmount]          DECIMAL(18,4)     NULL,           -- อากรที่จ่าย/ที่ขอคืน
    [VATAmount]           DECIMAL(18,4)     NULL,           -- ภาษีมูลค่าเพิ่ม

    -- อ้างอิงสิทธิ
    [ImportTaxIncId]      NVARCHAR(50)      NULL,           -- Import Tax Incentive ID (19ทวิ)
    [BOICardNo]           NVARCHAR(100)     NULL,           -- เลขที่บัตร BOI
    [ProductionFormulaNo] NVARCHAR(50)      NULL,           -- เลขที่สูตรการผลิต
    [CompensationNo]      NVARCHAR(50)      NULL,           -- เลขที่ชดเชย
    [TransferTableNo]     NVARCHAR(50)      NULL,           -- เลขที่ตารางโอนสิทธิ์

    -- โอนสิทธิ์
    [TransferFromCompany] NVARCHAR(200)     NULL,           -- บริษัทต้นทาง (โอนมาจาก)
    [TransferToCompany]   NVARCHAR(200)     NULL,           -- บริษัทปลายทาง (โอนไปให้)
    [TransferStockCardId] INT               NULL,           -- อ้างอิง stock_card Id ของรายการโอน

    -- Lot tracking (สำหรับ FIFO)
    [LotId]               INT               NULL,           -- อ้างอิง stock_lot.Id
    [LotImportDeclarNo]   NVARCHAR(50)      NULL,           -- เลขที่ใบขนของ Lot ที่ตัด

    -- Audit
    [CreatedBy]           NVARCHAR(100)     NULL,
    [CreatedDate]         DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
    [Remark]              NVARCHAR(500)     NULL,

    CONSTRAINT [PK_imp_stock_card] PRIMARY KEY CLUSTERED ([Id])
)

CREATE INDEX [IX_stock_card_material_date] ON [imp].[stock_card] ([RawMaterialCode], [TransactionDate])
CREATE INDEX [IX_stock_card_import] ON [imp].[stock_card] ([ImportDeclarNo], [ImportItemNo])
CREATE INDEX [IX_stock_card_export] ON [imp].[stock_card] ([ExportDeclarNo], [ExportItemNo])
CREATE INDEX [IX_stock_card_privilege] ON [imp].[stock_card] ([PrivilegeType])
CREATE INDEX [IX_stock_card_lot] ON [imp].[stock_card] ([LotId])
```

### 2.2 imp.stock_lot — Lot วัตถุดิบ (FIFO Queue)

```sql
CREATE TABLE [imp].[stock_lot] (
    [Id]                  INT IDENTITY(1,1) NOT NULL,
    [ImportDeclarNo]      NVARCHAR(50)      NOT NULL,       -- เลขที่ใบขนขาเข้า
    [ImportItemNo]        INT               NOT NULL,       -- ลำดับรายการ
    [ImportDate]          DATE              NOT NULL,       -- วันนำเข้า (FIFO sort key)
    [PrivilegeType]       NVARCHAR(20)      NOT NULL,       -- '19TVIS', 'BOI', 'COMPENSATION', 'TRANSFER'

    -- วัตถุดิบ
    [RawMaterialCode]     NVARCHAR(50)      NOT NULL,       -- รหัสวัตถุดิบ
    [ProductCode]         NVARCHAR(50)      NULL,           -- รหัสสินค้า
    [ProductDescription]  NVARCHAR(500)     NULL,           -- ชื่อ
    [Unit]                NVARCHAR(20)      NOT NULL,       -- หน่วย

    -- ปริมาณ
    [QtyOriginal]         DECIMAL(18,6)     NOT NULL,       -- ปริมาณนำเข้าเดิม
    [QtyUsed]             DECIMAL(18,6)     NOT NULL DEFAULT 0, -- ปริมาณที่ตัดไปแล้ว
    [QtyBalance]          DECIMAL(18,6)     NOT NULL,       -- คงเหลือ = Original - Used
    [QtyTransferred]      DECIMAL(18,6)     NOT NULL DEFAULT 0, -- ปริมาณที่โอนสิทธิ์

    -- มูลค่า/อากร (ต่อหน่วย เพื่อคำนวณคืน)
    [UnitPrice]           DECIMAL(18,4)     NULL,
    [CIFValueTHB]         DECIMAL(18,4)     NULL,           -- มูลค่า CIF ทั้ง lot
    [DutyRate]            DECIMAL(18,4)     NULL,           -- อัตราอากร
    [DutyPerUnit]         DECIMAL(18,6)     NULL,           -- อากรต่อหน่วย (คำนวณ)
    [TotalDutyVAT]        DECIMAL(18,4)     NULL,           -- อากร+VAT ทั้ง lot

    -- อ้างอิงสิทธิ
    [ImportTaxIncId]      NVARCHAR(50)      NULL,           -- สำหรับ 19ทวิ
    [BOICardNo]           NVARCHAR(100)     NULL,           -- สำหรับ BOI
    [ProductionFormulaNo] NVARCHAR(50)      NULL,           -- สูตรการผลิต

    -- สถานะ
    [Status]              NVARCHAR(20)      NOT NULL DEFAULT 'ACTIVE',
                                                            -- 'ACTIVE' = ยังมีคงเหลือ
                                                            -- 'DEPLETED' = หมดแล้ว
                                                            -- 'EXPIRED' = หมดอายุ (เกิน 1 ปี)
                                                            -- 'TRANSFERRED' = โอนสิทธิ์ออกหมด
    [ExpiryDate]          DATE              NULL,           -- วันหมดอายุสิทธิ (ImportDate + 1 ปี)

    -- Audit
    [CreatedBy]           NVARCHAR(100)     NULL,
    [CreatedDate]         DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_imp_stock_lot] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_imp_stock_lot] UNIQUE ([ImportDeclarNo], [ImportItemNo])
)

CREATE INDEX [IX_stock_lot_fifo] ON [imp].[stock_lot] ([RawMaterialCode], [PrivilegeType], [ImportDate], [Status])
CREATE INDEX [IX_stock_lot_privilege] ON [imp].[stock_lot] ([PrivilegeType], [Status])
CREATE INDEX [IX_stock_lot_expiry] ON [imp].[stock_lot] ([ExpiryDate], [Status])
```

### 2.3 imp.stock_cutting — รายละเอียดการตัด Stock (Lot ↔ Export)

```sql
CREATE TABLE [imp].[stock_cutting] (
    [Id]                  INT IDENTITY(1,1) NOT NULL,
    [StockLotId]          INT               NOT NULL,       -- FK → stock_lot.Id (Lot ที่ถูกตัด)
    [ExportDeclarNo]      NVARCHAR(50)      NOT NULL,       -- ใบขนขาออก
    [ExportItemNo]        INT               NOT NULL,       -- ลำดับรายการ
    [ExportDate]          DATE              NOT NULL,       -- วันส่งออก
    [PrivilegeType]       NVARCHAR(20)      NOT NULL,       -- '19TVIS', 'BOI', 'COMPENSATION', 'TRANSFER'

    -- สูตรการผลิต
    [ProductionFormulaNo] NVARCHAR(50)      NULL,           -- สูตรที่ใช้
    [BomDetailNo]         INT               NULL,           -- ลำดับรายการใน BOM

    -- วัตถุดิบที่ตัด
    [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
    [Unit]                NVARCHAR(20)      NOT NULL,

    -- ปริมาณ
    [ExportQty]           DECIMAL(18,6)     NOT NULL,       -- ปริมาณส่งออก (สินค้าสำเร็จรูป)
    [Ratio]               DECIMAL(18,6)     NOT NULL,       -- อัตราส่วนจาก BOM
    [Scrap]               DECIMAL(18,6)     NOT NULL DEFAULT 0, -- เศษเสียจาก BOM
    [QtyRequired]         DECIMAL(18,6)     NOT NULL,       -- วัตถุดิบที่ต้องใช้ = ExportQty × (Ratio + Scrap)
    [QtyCut]              DECIMAL(18,6)     NOT NULL,       -- ปริมาณที่ตัดจาก Lot นี้

    -- อากรที่ขอคืน (เฉพาะ 19ทวิ/โอนสิทธิ์)
    [DutyPerUnit]         DECIMAL(18,6)     NULL,           -- อากรต่อหน่วยของ Lot
    [DutyRefund]          DECIMAL(18,4)     NULL,           -- อากรที่ขอคืน = QtyCut × DutyPerUnit

    -- ชดเชย (เฉพาะชดเชยภาษี)
    [FOBValueTHB]         DECIMAL(18,4)     NULL,           -- FOB บาท
    [CompensationRate]    DECIMAL(18,4)     NULL,           -- อัตราชดเชย (%)
    [CompensationAmount]  DECIMAL(18,4)     NULL,           -- เงินชดเชย

    -- โอนสิทธิ์
    [TransferTableNo]     NVARCHAR(50)      NULL,           -- เลขที่ตารางโอนสิทธิ์
    [TransferFromCompany] NVARCHAR(200)     NULL,

    -- สถานะ
    [Status]              NVARCHAR(20)      NOT NULL DEFAULT 'PENDING',
                                                            -- 'PENDING' = รอยืนยัน
                                                            -- 'CONFIRMED' = ยืนยันแล้ว
                                                            -- 'CANCELLED' = ยกเลิก

    -- Audit
    [CreatedBy]           NVARCHAR(100)     NULL,
    [CreatedDate]         DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
    [ConfirmedBy]         NVARCHAR(100)     NULL,
    [ConfirmedDate]       DATETIME2(7)      NULL,

    CONSTRAINT [PK_imp_stock_cutting] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_stock_cutting_lot] FOREIGN KEY ([StockLotId])
        REFERENCES [imp].[stock_lot] ([Id])
)

CREATE INDEX [IX_stock_cutting_lot] ON [imp].[stock_cutting] ([StockLotId])
CREATE INDEX [IX_stock_cutting_export] ON [imp].[stock_cutting] ([ExportDeclarNo], [ExportItemNo])
CREATE INDEX [IX_stock_cutting_status] ON [imp].[stock_cutting] ([Status])
```

---

## 3. FIFO Cutting Logic (ขั้นตอนการตัด)

### 3.1 มาตรา 19 ทวิ — คืนอากร

```
Input:  ExportDeclarNo, ExportItemNo, ExportQty, ProductionFormulaNo
Output: รายการตัด stock + อากรที่ขอคืน

Step 1: ดึงสูตรการผลิต
   SELECT * FROM imp.bom_m29_dt
   WHERE BomM29HdId = (SELECT Id FROM imp.bom_m29_hd WHERE ProductionFormulaNo = @FormulaNo)

Step 2: วนแต่ละวัตถุดิบในสูตร
   FOR EACH material IN bom_m29_dt:
     qtyRequired = ExportQty × (material.Ratio + material.Scrap)

Step 3: ตัด FIFO — ดึง Lot เก่าสุดที่ยังเหลือ
   SELECT * FROM imp.stock_lot
   WHERE RawMaterialCode = @MaterialCode
     AND PrivilegeType = '19TVIS'
     AND Status = 'ACTIVE'
     AND ExpiryDate >= @ExportDate        -- ยังไม่หมดอายุ
   ORDER BY ImportDate ASC                -- FIFO: เก่าสุดก่อน

Step 4: ตัดจาก Lot ทีละ Lot จนครบ
   remaining = qtyRequired
   WHILE remaining > 0 AND has more lots:
     lot = next lot
     cutQty = MIN(remaining, lot.QtyBalance)

     -- บันทึก stock_cutting
     INSERT stock_cutting (StockLotId, QtyCut, DutyRefund = cutQty × lot.DutyPerUnit, ...)

     -- อัพเดท Lot
     UPDATE stock_lot SET QtyUsed += cutQty, QtyBalance -= cutQty
     IF QtyBalance = 0 → SET Status = 'DEPLETED'

     remaining -= cutQty

Step 5: บันทึก stock_card (OUT)
   INSERT stock_card (TransactionType='OUT', QtyOut=qtyRequired, ...)

Step 6: คืนผลลัพธ์
   RETURN totalDutyRefund = SUM(all cutting.DutyRefund)
```

### 3.2 BOI — ตัดบัญชี

```
เหมือน 19 ทวิ แต่:
- ใช้สูตร bom_boi_hd / bom_boi_dt
- PrivilegeType = 'BOI'
- กรอง Lot ตาม BOICardNo (ต้องตรงกับบัตรส่งเสริม)
- ไม่คำนวณ DutyRefund (เพราะไม่ได้จ่ายอากร)
- บันทึกเพื่อ "ตัดบัญชี" พิสูจน์การใช้วัตถุดิบ

  WHERE PrivilegeType = 'BOI'
    AND BOICardNo = @BOICardNo
    AND Status = 'ACTIVE'
  ORDER BY ImportDate ASC
```

### 3.3 ชดเชยภาษี — เงินชดเชย

```
แตกต่างจาก 19 ทวิ/BOI:
- ไม่ต้องตัด FIFO ต่อ Lot (ไม่ผูก Import โดยตรง)
- แต่ยังต้องตรวจว่ามี Stock เพียงพอ
- คำนวณจาก FOB × อัตราชดเชย

Step 1: ดึงสูตรการผลิต (ใช้ bom_m29 หรือสูตรเฉพาะ)
Step 2: คำนวณวัตถุดิบที่ต้องใช้
Step 3: ตรวจสอบ Stock คงเหลือรวม (ไม่ต้องระบุ Lot)
   SELECT SUM(QtyBalance) FROM imp.stock_lot
   WHERE RawMaterialCode = @MaterialCode AND Status = 'ACTIVE'
   IF SUM < qtyRequired → Error: วัตถุดิบไม่เพียงพอ

Step 4: ตัด FIFO (เพื่อบันทึกเป็นหลักฐาน แต่ไม่ใช้คำนวณอากร)
Step 5: คำนวณเงินชดเชย
   compensationAmount = FOBTHB × compensationRate

Step 6: บันทึก stock_card + stock_cutting
```

### 3.4 โอนสิทธิ์ 19 ทวิ — ข้ามบริษัท

```
ขั้นตอนที่ 1: บริษัท A โอนออก (Transfer Out)
   - เลือก Lot ที่จะโอน (FIFO หรือระบุ Lot)
   - บันทึก stock_card: TransactionType = 'TRANSFER_OUT'
   - อัพเดท stock_lot: QtyTransferred += transferQty, QtyBalance -= transferQty
   - สร้างเลขที่ตารางโอนสิทธิ์

ขั้นตอนที่ 2: บริษัท B รับโอน (Transfer In)
   - บันทึก stock_lot ใหม่: PrivilegeType = 'TRANSFER'
   - บันทึก stock_card: TransactionType = 'TRANSFER_IN'
   - อ้างอิง TransferTableNo + TransferFromCompany

ขั้นตอนที่ 3: บริษัท B ส่งออก
   - ตัด FIFO เหมือน 19 ทวิ แต่ใช้ PrivilegeType = 'TRANSFER'
   - DutyRefund คำนวณจากอากรของ Lot ต้นทาง (บริษัท A)
   - อ้างอิง TransferTableNo ในใบขนขาออก

Flow:
  บริษัท A                           บริษัท B
  ┌──────────┐                       ┌──────────┐
  │ stock_lot │ ──TRANSFER_OUT──→    │ stock_lot │ (ใหม่, TRANSFER)
  │ QtyBal ↓ │                       │ QtyBal ↑ │
  └──────────┘                       └────┬─────┘
                                          │
                                     ส่งออก + ตัด FIFO
                                          │
                                     ┌────▼─────┐
                                     │ cutting  │
                                     │ DutyRefund│
                                     └──────────┘
```

---

## 4. API Endpoints

### 4.1 Stock Lot Management
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/stock-lot` | ค้นหา Lot (filter: materialCode, privilegeType, status) |
| GET | `/api/stock-lot/{id}` | ดูรายละเอียด Lot + ประวัติตัด |
| POST | `/api/stock-lot/import-sync` | สร้าง Lot จาก import_excel (batch) |

### 4.2 Stock Cutting
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/stock-cutting/19tvis` | ตัด FIFO สิทธิ 19 ทวิ |
| POST | `/api/stock-cutting/boi` | ตัด FIFO สิทธิ BOI |
| POST | `/api/stock-cutting/compensation` | ตัดชดเชยภาษี |
| POST | `/api/stock-cutting/transfer` | ตัดโอนสิทธิ์ |
| GET | `/api/stock-cutting?exportDeclarNo=X` | ดูรายการตัดของใบขนขาออก |
| PUT | `/api/stock-cutting/{id}/confirm` | ยืนยันการตัด |
| PUT | `/api/stock-cutting/{id}/cancel` | ยกเลิกการตัด (คืน Qty กลับ Lot) |

### 4.3 Stock Card (รายงาน)
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/stock-card` | ค้นหา Stock Card (filter: materialCode, dateRange, type) |
| GET | `/api/stock-card/balance` | ยอดคงเหลือวัตถุดิบ (สรุปตาม material + privilege) |
| GET | `/api/stock-card/report/fifo` | รายงาน FIFO (วัตถุดิบรับ-จ่ายตาม Lot) |

### 4.4 Transfer (โอนสิทธิ์)
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/stock-transfer/out` | โอนสิทธิ์ออก |
| POST | `/api/stock-transfer/in` | รับโอนสิทธิ์เข้า |
| GET | `/api/stock-transfer` | ค้นหารายการโอน |

---

## 5. Request/Response DTOs

### 5.1 ตัด Stock 19 ทวิ
```json
// POST /api/stock-cutting/19tvis
{
  "exportDeclarNo": "A0051690104903",
  "exportItemNo": 1,
  "exportDate": "2026-01-06",
  "exportQty": 18655,
  "exportUnit": "KGM",
  "productionFormulaNo": "F-001",
  "importTaxIncentiveId": "D0402000002751"
}

// Response
{
  "cuttings": [
    {
      "lotId": 1,
      "importDeclarNo": "A0050690117021",
      "importDate": "2026-01-01",
      "rawMaterialCode": "LLDPE",
      "qtyCut": 15000.000000,
      "dutyPerUnit": 1.416000,
      "dutyRefund": 21240.00
    },
    {
      "lotId": 2,
      "importDeclarNo": "A0050690117035",
      "importDate": "2026-01-05",
      "rawMaterialCode": "LLDPE",
      "qtyCut": 4587.750000,
      "dutyPerUnit": 1.450000,
      "dutyRefund": 6652.24
    }
  ],
  "totalQtyRequired": 19587.750000,
  "totalDutyRefund": 27892.24
}
```

### 5.2 ตัด Stock ชดเชย
```json
// POST /api/stock-cutting/compensation
{
  "exportDeclarNo": "A0051690104903",
  "exportItemNo": 1,
  "exportDate": "2026-01-06",
  "exportQty": 18655,
  "exportUnit": "KGM",
  "productionFormulaNo": "F-001",
  "fobValueTHB": 1400927.77,
  "compensationRate": 0.5,
  "compensationNo": "07-07799"
}

// Response
{
  "stockSufficient": true,
  "totalQtyRequired": 19587.750000,
  "compensationAmount": 7004.64,
  "cuttings": [ ... ]
}
```

---

## 6. Frontend Pages

### 6.1 Stock Card หลัก (`/stockcard`)
- แสดงยอดคงเหลือวัตถุดิบ (สรุปตาม material + สิทธิ)
- ค้นหาตาม: รหัสวัตถุดิบ, ช่วงวันที่, ประเภทสิทธิ
- Drill-down ดูรายการรับ-จ่ายตาม Lot

### 6.2 ตัด Stock (`/stockcard/cutting`)
- เลือกใบขนขาออก → ระบบดึงข้อมูลสินค้า + สูตรการผลิต
- กด "ตัด Stock" → ระบบคำนวณ FIFO อัตโนมัติ
- แสดง Preview: Lot ที่จะถูกตัด, ปริมาณ, อากรที่ขอคืน
- กด "ยืนยัน" → บันทึก

### 6.3 โอนสิทธิ์ (`/stockcard/transfer`)
- ฟอร์มโอนออก: เลือก Lot + ปริมาณ + บริษัทปลายทาง
- ฟอร์มรับโอน: ใส่เลขที่ตารางโอนสิทธิ์

### 6.4 รายงาน (`/report`)
- Stock Card รายวัตถุดิบ (รับ-จ่ายตาม FIFO)
- สรุปอากรที่ขอคืนตามสิทธิ
- รายการ Lot ที่ใกล้หมดอายุ
- สรุปการโอนสิทธิ์

---

## 7. Backend Layering

```
Controllers/
├── StockLotController.cs
├── StockCuttingController.cs
├── StockCardController.cs
└── StockTransferController.cs

Services/
├── IStockLotService.cs / StockLotService.cs
├── IStockCuttingService.cs / StockCuttingService.cs       ← FIFO logic อยู่ที่นี่
├── IStockCardService.cs / StockCardService.cs
└── IStockTransferService.cs / StockTransferService.cs

Repositories/
├── IStockLotRepository.cs / StockLotRepository.cs
├── IStockCuttingRepository.cs / StockCuttingRepository.cs
├── IStockCardRepository.cs / StockCardRepository.cs
└── IStockTransferRepository.cs / StockTransferRepository.cs

Models/
├── StockLot.cs
├── StockCutting.cs
├── StockCard.cs
└── StockTransfer.cs (ถ้าแยก)

DTOs/
├── StockLotDtos.cs
├── StockCuttingDtos.cs
├── StockCardDtos.cs
└── StockTransferDtos.cs
```

---

## 8. Implementation Order (ลำดับการพัฒนา)

### Phase 1: Foundation
1. สร้าง SQL tables (`stock_lot`, `stock_card`, `stock_cutting`)
2. สร้าง Models + DTOs
3. สร้าง Repositories (CRUD)
4. สร้าง Stock Lot sync จาก import_excel → stock_lot

### Phase 2: FIFO Core
5. สร้าง StockCuttingService — FIFO logic สำหรับ 19 ทวิ
6. สร้าง Controller + ทดสอบ API
7. สร้าง Frontend Stock Card หน้าหลัก (ดูยอดคงเหลือ)
8. สร้าง Frontend หน้าตัด Stock

### Phase 3: Multi-Privilege
9. เพิ่ม FIFO logic สำหรับ BOI (คล้าย 19 ทวิ + filter BOICardNo)
10. เพิ่ม logic ชดเชยภาษี (ตรวจ stock + คำนวณจาก FOB)
11. เพิ่ม logic โอนสิทธิ์ (Transfer Out/In + ตัด FIFO)

### Phase 4: Reports & Polish
12. หน้ารายงาน Stock Card (รับ-จ่ายตาม Lot)
13. รายงานสรุปอากรที่ขอคืน
14. แจ้งเตือน Lot ใกล้หมดอายุ
15. Export รายงานเป็น Excel

---

## 9. Business Rules สำคัญ

| # | Rule | รายละเอียด |
|---|------|-----------|
| 1 | FIFO เสมอ | ตัดจาก Lot วันนำเข้าเก่าสุดก่อน ภายใต้สิทธิเดียวกัน |
| 2 | ไม่ตัดข้ามสิทธิ | Lot ที่นำเข้าสิทธิ BOI ใช้ตัดได้เฉพาะ Export ที่ใช้สิทธิ BOI |
| 3 | หมดอายุ 1 ปี | 19 ทวิ: Lot ที่เกิน 1 ปี จากวันนำเข้า ไม่สามารถตัดได้ |
| 4 | BOI ตามบัตร | ตัดได้เฉพาะ Lot ที่อยู่ภายใต้บัตรส่งเสริมเดียวกัน |
| 5 | ชดเชยตรวจ Stock | ต้องมี Stock เพียงพอ แม้ไม่ต้องผูก Lot โดยตรง |
| 6 | โอนสิทธิ์ต้องมี Lot | โอนได้เฉพาะ Lot ที่ยังมี QtyBalance > 0 |
| 7 | ยกเลิกตัดได้ | ถ้า Status = PENDING → ยกเลิกและคืน Qty กลับ Lot |
| 8 | ไม่ตัดซ้ำ | Export 1 รายการ + วัตถุดิบ 1 ชนิด ตัดได้ครั้งเดียว |
| 9 | 1 Export = 1 สิทธิ | แต่ละรายการส่งออกใช้ได้เพียง 1 ประเภทสิทธิ |
| 10 | สูตรต้องมี | ต้องมีสูตรการผลิตก่อนตัด Stock ได้ |
