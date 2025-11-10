import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, NgIf, NgFor, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CashMonitoringService, CashMonitoringQuery, CashMonitoringResult } from '../../services/cash-monitoring.service';

@Component({
  standalone: true,
  selector: 'app-cash-monitoring',
  imports: [CommonModule, FormsModule, NgIf, NgFor, DecimalPipe],
  templateUrl: './cash-monitoring.component.html'
})
export class CashMonitoringComponent implements OnInit {
  private readonly service = inject(CashMonitoringService);

  rows: CashMonitoringResult['Items'] = [];
  footer?: CashMonitoringResult['Footer'];
  total = 0;
  loading = false;
  error: string | null = null;

  years: number[] = [];
  query: CashMonitoringQuery = { year: new Date().getFullYear(), page: 1, pageSize: 10 };
  pageSizes = [10, 20];

  ngOnInit(): void {
    const now = new Date().getFullYear();
    this.years = [now - 1, now, now + 1];
    this.load();
  }

  load(): void {
    this.loading = true;
    this.service.list(this.query).subscribe({
      next: (res) => {
        this.rows = res.Items;
        this.footer = res.Footer;
        this.total = res.Total;
        this.loading = false;
        this.error = null;
      },
      error: () => {
        this.error = 'Tidak dapat memuat monitoring kas.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.query.page = 1;
    this.load();
  }

  resetFilters(): void {
    const y = this.query.year;
    this.query = { year: y, page: 1, pageSize: this.query.pageSize };
    this.load();
  }

  changePageSize(size: number): void {
    this.query.pageSize = size;
    this.query.page = 1;
    this.load();
  }

  prevPage(): void {
    if ((this.query.page || 1) > 1) {
      this.query.page = (this.query.page || 1) - 1;
      this.load();
    }
  }

  nextPage(): void {
    if ((this.query.page || 1) < this.totalPages) {
      this.query.page = (this.query.page || 1) + 1;
      this.load();
    }
  }

  get totalPages(): number {
    const size = this.query.pageSize || 10;
    return Math.max(1, Math.ceil(this.total / size));
  }

  private formatId(n: number): string {
    try {
      return new Intl.NumberFormat('id-ID', { maximumFractionDigits: 0 }).format(n || 0);
    } catch {
      return String(n ?? 0);
    }
  }

  private buildCsv(): string {
    const sep = ';';
    const header = ['No', 'Blok', 'Nama', '1','2','3','4','5','6','7','8','9','10','11','12','Total'];
    const lines: string[] = [];
    lines.push(header.join(sep));
    const start = ((this.query.page || 1) - 1) * (this.query.pageSize || 10);
    this.rows.forEach((r, idx) => {
      const row = [
        String(start + idx + 1),
        r.Blok,
        '"' + (r.FullName ?? '').replace(/"/g, '""') + '"',
        r.M1, r.M2, r.M3, r.M4, r.M5, r.M6, r.M7, r.M8, r.M9, r.M10, r.M11, r.M12, r.Total
      ].map(v => typeof v === 'number' ? String(Math.trunc(v)) : String(v));
      lines.push(row.join(sep));
    });
    if (this.footer) {
      const f = this.footer;
      const footerRow = ['Total', '', '', f.M1, f.M2, f.M3, f.M4, f.M5, f.M6, f.M7, f.M8, f.M9, f.M10, f.M11, f.M12, f.Total]
        .map(v => typeof v === 'number' ? String(Math.trunc(v)) : String(v));
      lines.push(footerRow.join(sep));
    }
    return lines.join('\r\n');
  }

  exportExcel(): void {
    const csv = this.buildCsv();
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `monitoring_kas_${this.query.year || ''}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  private buildPrintHtml(): string {
    const style = `
      body { font-family: Arial, sans-serif; }
      h3 { margin: 0 0 12px 0; }
      table { border-collapse: collapse; width: 100%; }
      th, td { border: 1px solid #ccc; padding: 6px 8px; font-size: 12px; }
      th { background: #f5f5f5; }
      tfoot th { font-weight: bold; }
      .text-end { text-align: right; }
    `;
    const header = ['No','Blok','Nama', ...Array.from({length:12}, (_,i)=>String(i+1)), 'Total'];
    const start = ((this.query.page || 1) - 1) * (this.query.pageSize || 10);
    const rowsHtml = this.rows.map((r, idx) => `
      <tr>
        <td>${start + idx + 1}</td>
        <td>${r.Blok}</td>
        <td>${r.FullName ?? ''}</td>
        <td class="text-end">${this.formatId(r.M1)}</td>
        <td class="text-end">${this.formatId(r.M2)}</td>
        <td class="text-end">${this.formatId(r.M3)}</td>
        <td class="text-end">${this.formatId(r.M4)}</td>
        <td class="text-end">${this.formatId(r.M5)}</td>
        <td class="text-end">${this.formatId(r.M6)}</td>
        <td class="text-end">${this.formatId(r.M7)}</td>
        <td class="text-end">${this.formatId(r.M8)}</td>
        <td class="text-end">${this.formatId(r.M9)}</td>
        <td class="text-end">${this.formatId(r.M10)}</td>
        <td class="text-end">${this.formatId(r.M11)}</td>
        <td class="text-end">${this.formatId(r.M12)}</td>
        <td class="text-end">${this.formatId(r.Total)}</td>
      </tr>`).join('');
    const footer = this.footer ? `
      <tfoot>
        <tr>
          <th colspan="3" class="text-end">Total</th>
          <th class="text-end">${this.formatId(this.footer.M1)}</th>
          <th class="text-end">${this.formatId(this.footer.M2)}</th>
          <th class="text-end">${this.formatId(this.footer.M3)}</th>
          <th class="text-end">${this.formatId(this.footer.M4)}</th>
          <th class="text-end">${this.formatId(this.footer.M5)}</th>
          <th class="text-end">${this.formatId(this.footer.M6)}</th>
          <th class="text-end">${this.formatId(this.footer.M7)}</th>
          <th class="text-end">${this.formatId(this.footer.M8)}</th>
          <th class="text-end">${this.formatId(this.footer.M9)}</th>
          <th class="text-end">${this.formatId(this.footer.M10)}</th>
          <th class="text-end">${this.formatId(this.footer.M11)}</th>
          <th class="text-end">${this.formatId(this.footer.M12)}</th>
          <th class="text-end">${this.formatId(this.footer.Total)}</th>
        </tr>
      </tfoot>` : '';
    return `<!doctype html><html><head><meta charset="utf-8"><title>Monitoring Kas</title>
      <style>${style}</style></head><body>
      <h3>Monitoring Kas ${this.query.year || ''}</h3>
      <table><thead><tr>${header.map(h=>`<th>${h}</th>`).join('')}</tr></thead>
      <tbody>${rowsHtml}</tbody>${footer}</table>
      </body></html>`;
  }

  exportPdf(): void {
    const html = this.buildPrintHtml();
    const win = window.open('', '_blank');
    if (!win) return;
    win.document.open();
    win.document.write(html);
    win.document.close();
    setTimeout(() => { try { win.focus(); win.print(); } catch {} }, 300);
  }
}
