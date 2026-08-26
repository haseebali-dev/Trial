import { Component, OnInit, OnDestroy, AfterViewInit, Input, OnChanges, SimpleChanges, ViewChild, ElementRef, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { createChart, IChartApi, ISeriesApi, CandlestickData, HistogramData, LineData, UTCTimestamp, ColorType, CandlestickSeries, HistogramSeries, LineSeries, CandlestickSeriesOptions, HistogramSeriesOptions, LineSeriesOptions } from 'lightweight-charts';
import { Candle, Timeframe, EmaDto, BollingerBandsDto, VwapDto } from '../../../core/models/market.models';

@Component({
  selector: 'app-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div #chartContainer class="chart-container"></div>
    <div class="chart-toolbar" *ngIf="showToolbar">
      <div class="timeframe-buttons">
        <button
          *ngFor="let tf of timeframes"
          class="timeframe-btn"
          [class.active]="currentTimeframe === tf"
          (click)="onTimeframeChange(tf)">
          {{ tf }}
        </button>
      </div>
      <div class="indicator-toggles">
        <button class="indicator-btn" [class.active]="showEma9" (click)="toggleEma9()" title="EMA 9">
          EMA 9
        </button>
        <button class="indicator-btn" [class.active]="showEma21" (click)="toggleEma21()" title="EMA 21">
          EMA 21
        </button>
        <button class="indicator-btn" [class.active]="showEma50" (click)="toggleEma50()" title="EMA 50">
          EMA 50
        </button>
        <button class="indicator-btn" [class.active]="showBollingerBands" (click)="toggleBollingerBands()" title="Bollinger Bands">
          BB
        </button>
        <button class="indicator-btn" [class.active]="showVwap" (click)="toggleVwap()" title="VWAP">
          VWAP
        </button>
      </div>
      <div class="chart-controls">
        <button class="control-btn" (click)="resetChart()" title="Reset View">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
            <polyline points="9 22 9 12 15 12 15 22"></polyline>
          </svg>
        </button>
        <button class="control-btn" (click)="toggleCrosshair()" title="Toggle Crosshair">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="22" y1="12" x2="18" y2="12"></line>
            <line x1="6" y1="12" x2="2" y2="12"></line>
            <line x1="12" y1="6" x2="12" y2="2"></line>
            <line x1="12" y1="22" x2="12" y2="18"></line>
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .chart-container {
      width: 100%;
      height: 100%;
      min-height: 400px;
      background: #131722;
    }

    .chart-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 8px 12px;
      background: #131722;
      border-top: 1px solid #2a2e39;
    }

    .timeframe-buttons {
      display: flex;
      gap: 4px;
    }

    .timeframe-btn {
      padding: 4px 10px;
      background: transparent;
      border: 1px solid #2a2e39;
      border-radius: 4px;
      color: #787b86;
      font-size: 0.75rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .timeframe-btn:hover {
      border-color: #3d4251;
      color: #d1d4dc;
    }

    .timeframe-btn.active {
      background: #2962ff;
      border-color: #2962ff;
      color: #fff;
    }

    .chart-controls {
      display: flex;
      gap: 4px;
    }

    .control-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      background: transparent;
      border: 1px solid #2a2e39;
      border-radius: 4px;
      color: #787b86;
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .control-btn:hover {
      border-color: #3d4251;
      color: #d1d4dc;
      background: #1e222d;
    }

    .indicator-toggles {
      display: flex;
      gap: 4px;
    }

    .indicator-btn {
      padding: 4px 8px;
      background: transparent;
      border: 1px solid #2a2e39;
      border-radius: 4px;
      color: #787b86;
      font-size: 0.7rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .indicator-btn:hover {
      border-color: #3d4251;
      color: #d1d4dc;
    }

    .indicator-btn.active {
      background: #2962ff;
      border-color: #2962ff;
      color: #fff;
    }
  `]
})
export class ChartComponent implements OnInit, OnDestroy, AfterViewInit, OnChanges {
  @ViewChild('chartContainer', { static: true }) chartContainer!: ElementRef<HTMLDivElement>;
  @Input() symbol = 'BTCUSDT';
  @Input() timeframe: Timeframe = '1h';
  @Input() candles: Candle[] = [];
  @Input() showToolbar = true;
  @Input() ema9: EmaDto[] = [];
  @Input() ema21: EmaDto[] = [];
  @Input() ema50: EmaDto[] = [];
  @Input() bollingerBands: BollingerBandsDto[] = [];
  @Input() vwap: VwapDto[] = [];
  @Input() showEma9 = false;
  @Input() showEma21 = false;
  @Input() showEma50 = false;
  @Input() showBollingerBands = false;
  @Input() showVwap = false;
  @Output() timeframeChange = new EventEmitter<Timeframe>();

  timeframes: Timeframe[] = ['1m', '5m', '15m', '30m', '1h', '4h', '1d'];
  currentTimeframe: Timeframe = '1h';

  private chart!: IChartApi;
  private candleSeries!: ISeriesApi<'Candlestick'>;
  private volumeSeries!: ISeriesApi<'Histogram'>;
  private ema9Series!: ISeriesApi<'Line'>;
  private ema21Series!: ISeriesApi<'Line'>;
  private ema50Series!: ISeriesApi<'Line'>;
  private bbUpperSeries!: ISeriesApi<'Line'>;
  private bbMiddleSeries!: ISeriesApi<'Line'>;
  private bbLowerSeries!: ISeriesApi<'Line'>;
  private vwapSeries!: ISeriesApi<'Line'>;
  private crosshairEnabled = true;
  private resizeObserver!: ResizeObserver;
  private isDestroyed = false;

  ngOnInit(): void {
    this.currentTimeframe = this.timeframe;
  }

  ngAfterViewInit(): void {
    this.initializeChart();
    // updateChart will be called inside initializeChart after series are created
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.chart) {
      if (changes['candles'] || changes['timeframe']) {
        this.updateChart();
      }
      if (changes['timeframe']) {
        this.currentTimeframe = this.timeframe;
      }
    } else {
      // Chart not initialized yet, store the changes to apply later
      if (changes['candles'] && !changes['candles'].firstChange) {
        this.candles = changes['candles'].currentValue || [];
      }
      if (changes['timeframe']) {
        this.currentTimeframe = changes['timeframe'].currentValue;
      }
    }
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
    if (this.chart) {
      this.chart.remove();
    }
  }

  private initializeChart(): void {
    const container = this.chartContainer.nativeElement;

    this.chart = createChart(container, {
      width: container.clientWidth,
      height: container.clientHeight,
      layout: {
        background: { type: ColorType.Solid, color: '#131722' },
        textColor: '#d1d4dc',
        fontSize: 11,
        fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
      },
      grid: {
        vertLines: { color: '#2a2e39' },
        horzLines: { color: '#2a2e39' },
      },
      crosshair: {
        mode: 1, // Normal
        vertLine: { color: '#787b86', width: 1, style: 1, labelBackgroundColor: '#2a2e39' },
        horzLine: { color: '#787b86', width: 1, style: 1, labelBackgroundColor: '#2a2e39' },
      },
      rightPriceScale: {
        borderColor: '#2a2e39',
        scaleMargins: { top: 0.1, bottom: 0.25 },
      },
      timeScale: {
        borderColor: '#2a2e39',
        timeVisible: true,
        secondsVisible: false,
        tickMarkFormatter: (time: UTCTimestamp) => {
          const date = new Date(time * 1000);
          return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        },
      },
      handleScroll: {
        mouseWheel: true,
        pressedMouseMove: true,
        horzTouchDrag: true,
        vertTouchDrag: true,
      },
      handleScale: {
        mouseWheel: true,
        pinch: true,
        axisPressedMouseMove: { time: true, price: true },
      },
    });

    // Create candle series
    this.candleSeries = this.chart.addSeries(CandlestickSeries, {
      upColor: '#48bb78',
      downColor: '#e53e3e',
      borderUpColor: '#48bb78',
      borderDownColor: '#e53e3e',
      wickUpColor: '#48bb78',
      wickDownColor: '#e53e3e',
      priceFormat: {
        type: 'price',
        precision: 2,
        minMove: 0.01,
      },
    } as CandlestickSeriesOptions);

    // Create volume series (histogram)
    this.volumeSeries = this.chart.addSeries(HistogramSeries, {
      color: '#2962ff',
      priceFormat: {
        type: 'volume',
        precision: 0,
      },
      priceScaleId: 'volume',
      scaleMargins: { top: 0.75, bottom: 0 },
      base: 0,
      visible: true,
      lastValueVisible: false,
      title: 'Volume',
      priceLineVisible: false,
    } as unknown as HistogramSeriesOptions);

    // Create EMA 9 series (blue)
    this.ema9Series = this.chart.addSeries(LineSeries, {
      color: '#2962ff',
      lineWidth: 1,
      priceLineVisible: false,
      lastValueVisible: false,
      title: 'EMA 9',
    } as LineSeriesOptions);

    // Create EMA 21 series (orange)
    this.ema21Series = this.chart.addSeries(LineSeries, {
      color: '#f39c12',
      lineWidth: 1,
      priceLineVisible: false,
      lastValueVisible: false,
      title: 'EMA 21',
    } as LineSeriesOptions);

    // Create EMA 50 series (purple)
    this.ema50Series = this.chart.addSeries(LineSeries, {
      color: '#9b59b6',
      lineWidth: 1,
      priceLineVisible: false,
      lastValueVisible: false,
      title: 'EMA 50',
    } as LineSeriesOptions);

    // Create Bollinger Bands Upper (gray dashed)
    this.bbUpperSeries = this.chart.addSeries(LineSeries, {
      color: '#7f8c8d',
      lineWidth: 1,
      lineStyle: 2, // Dashed
      priceLineVisible: false,
      lastValueVisible: false,
      title: 'BB Upper',
    } as LineSeriesOptions);

    // Create Bollinger Bands Middle (gray)
    this.bbMiddleSeries = this.chart.addSeries(LineSeries, {
      color: '#95a5a6',
      lineWidth: 1,
      priceLineVisible: false,
      lastValueVisible: false,
      title: 'BB Middle',
    } as LineSeriesOptions);

    // Create Bollinger Bands Lower (gray dashed)
    this.bbLowerSeries = this.chart.addSeries(LineSeries, {
      color: '#7f8c8d',
      lineWidth: 1,
      lineStyle: 2, // Dashed
      priceLineVisible: false,
      lastValueVisible: false,
      title: 'BB Lower',
    } as LineSeriesOptions);

    // Create VWAP series (yellow)
    this.vwapSeries = this.chart.addSeries(LineSeries, {
      color: '#f1c40f',
      lineWidth: 2,
      priceLineVisible: false,
      lastValueVisible: false,
      title: 'VWAP',
    } as LineSeriesOptions);

    // Initially hide all indicator series
    this.ema9Series.applyOptions({ visible: false });
    this.ema21Series.applyOptions({ visible: false });
    this.ema50Series.applyOptions({ visible: false });
    this.bbUpperSeries.applyOptions({ visible: false });
    this.bbMiddleSeries.applyOptions({ visible: false });
    this.bbLowerSeries.applyOptions({ visible: false });
    this.vwapSeries.applyOptions({ visible: false });

    // Subscribe to crosshair move for tooltip data
    this.chart.subscribeCrosshairMove((param) => {
      if (param.time && param.seriesData.get(this.candleSeries)) {
        const data = param.seriesData.get(this.candleSeries) as CandlestickData;
        // Could emit event for tooltip display
      }
    });

    // Handle container resize
    this.resizeObserver = new ResizeObserver(() => {
      if (this.chart && !this.isDestroyed) {
        this.chart.applyOptions({
          width: container.clientWidth,
          height: container.clientHeight,
        });
      }
    });
    this.resizeObserver.observe(container);

    // Load initial data if available
    this.updateChart();
  }

  private updateChart(): void {
    if (!this.candleSeries || !this.volumeSeries || !this.candles.length) {
      return;
    }

    // Convert candles to lightweight-charts format
    const candleData: CandlestickData[] = this.candles.map(c => ({
      time: c.timestamp / 1000 as UTCTimestamp, // Convert ms to seconds
      open: c.open,
      high: c.high,
      low: c.low,
      close: c.close,
    }));

    const volumeData: HistogramData[] = this.candles.map(c => ({
      time: c.timestamp / 1000 as UTCTimestamp,
      value: c.volume,
      color: c.close >= c.open ? 'rgba(72, 187, 120, 0.5)' : 'rgba(229, 62, 62, 0.5)',
    }));

    this.candleSeries.setData(candleData);
    this.volumeSeries.setData(volumeData);

    // Update indicator series if data is available
    this.updateIndicatorSeries();

    // Fit content on first load
    this.chart.timeScale().fitContent();
  }

  private updateIndicatorSeries(): void {
    // EMA 9
    if (this.ema9.length > 0) {
      const ema9Data: LineData[] = this.ema9.map(e => ({
        time: e.timestamp / 1000 as UTCTimestamp,
        value: e.value,
      }));
      this.ema9Series.setData(ema9Data);
      this.ema9Series.applyOptions({ visible: this.showEma9 });
    } else {
      this.ema9Series.applyOptions({ visible: false });
    }

    // EMA 21
    if (this.ema21.length > 0) {
      const ema21Data: LineData[] = this.ema21.map(e => ({
        time: e.timestamp / 1000 as UTCTimestamp,
        value: e.value,
      }));
      this.ema21Series.setData(ema21Data);
      this.ema21Series.applyOptions({ visible: this.showEma21 });
    } else {
      this.ema21Series.applyOptions({ visible: false });
    }

    // EMA 50
    if (this.ema50.length > 0) {
      const ema50Data: LineData[] = this.ema50.map(e => ({
        time: e.timestamp / 1000 as UTCTimestamp,
        value: e.value,
      }));
      this.ema50Series.setData(ema50Data);
      this.ema50Series.applyOptions({ visible: this.showEma50 });
    } else {
      this.ema50Series.applyOptions({ visible: false });
    }

    // Bollinger Bands
    if (this.bollingerBands.length > 0) {
      const bbUpperData: LineData[] = this.bollingerBands.map(b => ({
        time: b.timestamp / 1000 as UTCTimestamp,
        value: b.upper,
      }));
      const bbMiddleData: LineData[] = this.bollingerBands.map(b => ({
        time: b.timestamp / 1000 as UTCTimestamp,
        value: b.middle,
      }));
      const bbLowerData: LineData[] = this.bollingerBands.map(b => ({
        time: b.timestamp / 1000 as UTCTimestamp,
        value: b.lower,
      }));
      this.bbUpperSeries.setData(bbUpperData);
      this.bbMiddleSeries.setData(bbMiddleData);
      this.bbLowerSeries.setData(bbLowerData);
      const bbVisible = this.showBollingerBands;
      this.bbUpperSeries.applyOptions({ visible: bbVisible });
      this.bbMiddleSeries.applyOptions({ visible: bbVisible });
      this.bbLowerSeries.applyOptions({ visible: bbVisible });
    } else {
      this.bbUpperSeries.applyOptions({ visible: false });
      this.bbMiddleSeries.applyOptions({ visible: false });
      this.bbLowerSeries.applyOptions({ visible: false });
    }

    // VWAP
    if (this.vwap.length > 0) {
      const vwapData: LineData[] = this.vwap.map(v => ({
        time: v.timestamp / 1000 as UTCTimestamp,
        value: v.value,
      }));
      this.vwapSeries.setData(vwapData);
      this.vwapSeries.applyOptions({ visible: this.showVwap });
    } else {
      this.vwapSeries.applyOptions({ visible: false });
    }
  }

  onTimeframeChange(tf: Timeframe): void {
    this.currentTimeframe = tf;
    this.timeframeChange.emit(tf);
  }

  resetChart(): void {
    this.chart.timeScale().fitContent();
  }

  toggleCrosshair(): void {
    this.crosshairEnabled = !this.crosshairEnabled;
    this.chart.applyOptions({
      crosshair: { mode: this.crosshairEnabled ? 1 : 0 },
    });
  }

  // Indicator toggle methods
  toggleEma9(): void {
    this.showEma9 = !this.showEma9;
    if (this.ema9.length > 0) {
      this.ema9Series.applyOptions({ visible: this.showEma9 });
    }
  }

  toggleEma21(): void {
    this.showEma21 = !this.showEma21;
    if (this.ema21.length > 0) {
      this.ema21Series.applyOptions({ visible: this.showEma21 });
    }
  }

  toggleEma50(): void {
    this.showEma50 = !this.showEma50;
    if (this.ema50.length > 0) {
      this.ema50Series.applyOptions({ visible: this.showEma50 });
    }
  }

  toggleBollingerBands(): void {
    this.showBollingerBands = !this.showBollingerBands;
    if (this.bollingerBands.length > 0) {
      this.bbUpperSeries.applyOptions({ visible: this.showBollingerBands });
      this.bbMiddleSeries.applyOptions({ visible: this.showBollingerBands });
      this.bbLowerSeries.applyOptions({ visible: this.showBollingerBands });
    }
  }

  toggleVwap(): void {
    this.showVwap = !this.showVwap;
    if (this.vwap.length > 0) {
      this.vwapSeries.applyOptions({ visible: this.showVwap });
    }
  }

  // Public method to update data from parent
  updateData(candles: Candle[]): void {
    this.candles = candles;
    this.updateChart();
  }

  // Public method to update indicator data from parent
  updateIndicators(
    ema9: EmaDto[],
    ema21: EmaDto[],
    ema50: EmaDto[],
    bollingerBands: BollingerBandsDto[],
    vwap: VwapDto[]
  ): void {
    this.ema9 = ema9;
    this.ema21 = ema21;
    this.ema50 = ema50;
    this.bollingerBands = bollingerBands;
    this.vwap = vwap;
    this.updateIndicatorSeries();
  }
}