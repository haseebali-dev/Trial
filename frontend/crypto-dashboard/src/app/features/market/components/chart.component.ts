import { Component, OnInit, OnDestroy, AfterViewInit, Input, OnChanges, SimpleChanges, ViewChild, ElementRef, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { createChart, IChartApi, ISeriesApi, CandlestickData, HistogramData, UTCTimestamp, ColorType, CandlestickSeries, HistogramSeries, CandlestickSeriesOptions, HistogramSeriesOptions } from 'lightweight-charts';
import { Candle, Timeframe } from '../../../core/models/market.models';

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
  `]
})
export class ChartComponent implements OnInit, OnDestroy, AfterViewInit, OnChanges {
  @ViewChild('chartContainer', { static: true }) chartContainer!: ElementRef<HTMLDivElement>;
  @Input() symbol = 'BTCUSDT';
  @Input() timeframe: Timeframe = '1h';
  @Input() candles: Candle[] = [];
  @Input() showToolbar = true;
  @Output() timeframeChange = new EventEmitter<Timeframe>();

  timeframes: Timeframe[] = ['1m', '5m', '15m', '30m', '1h', '4h', '1d'];
  currentTimeframe: Timeframe = '1h';

  private chart!: IChartApi;
  private candleSeries!: ISeriesApi<'Candlestick'>;
  private volumeSeries!: ISeriesApi<'Histogram'>;
  private crosshairEnabled = true;

  ngOnInit(): void {
    this.currentTimeframe = this.timeframe;
  }

  ngAfterViewInit(): void {
    this.initializeChart();
    this.updateChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.chart) {
      if (changes['candles'] || changes['timeframe']) {
        this.updateChart();
      }
      if (changes['timeframe']) {
        this.currentTimeframe = this.timeframe;
      }
    }
  }

  ngOnDestroy(): void {
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

    // Subscribe to crosshair move for tooltip data
    this.chart.subscribeCrosshairMove((param) => {
      if (param.time && param.seriesData.get(this.candleSeries)) {
        const data = param.seriesData.get(this.candleSeries) as CandlestickData;
        // Could emit event for tooltip display
      }
    });

    // Handle container resize
    const resizeObserver = new ResizeObserver(() => {
      this.chart.applyOptions({
        width: container.clientWidth,
        height: container.clientHeight,
      });
    });
    resizeObserver.observe(container);
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

    // Fit content on first load
    this.chart.timeScale().fitContent();
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

  // Public method to update data from parent
  updateData(candles: Candle[]): void {
    this.candles = candles;
    this.updateChart();
  }
}