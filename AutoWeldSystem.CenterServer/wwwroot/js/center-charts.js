// 图表实例拥有自己的 DOM；Blazor 只传数据，不参与逐帧绘制。
export function create(element, kind, callback) {
    const chart = echarts.init(element, null, { renderer: 'canvas' });
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
    let rows = [];
    let disposed = false;
    let frame;

    function paint() {
        if (disposed || document.hidden || element.clientWidth === 0 || element.clientHeight === 0) return;
        const style = getComputedStyle(element);
        const color = name => style.getPropertyValue(name).trim();
        const ink = color('--ink');
        const muted = color('--ink-2');
        const surface = color('--surface');
        const accent = color('--chart-accent');
        const good = color('--chart-good');
        const bad = color('--chart-bad');
        const animated = !reducedMotion.matches && document.documentElement.dataset.motion !== 'off';
        const common = {
            animation: animated,
            animationDuration: 650,
            animationDurationUpdate: 380,
            animationEasingUpdate: 'cubicOut',
            backgroundColor: 'transparent',
            textStyle: { fontFamily: 'Microsoft YaHei UI, Segoe UI, sans-serif', color: ink },
            tooltip: {
                trigger: kind === 'quality' ? 'item' : 'axis',
                renderMode: 'richText',
                confine: true,
                backgroundColor: surface,
                borderColor: color('--hairline-strong'),
                textStyle: { color: ink, fontSize: 13 },
                padding: 12
            }
        };
        if (kind === 'quality') {
            const row = rows[0];
            const noData = !row || row.total == null || row.total === 0;
            chart.setOption({
                ...common,
                series: [{
                    id: 'quality', type: 'pie', radius: ['67%', '84%'], center: ['50%', '50%'],
                    silent: noData, label: { show: false }, labelLine: { show: false },
                    emphasis: { scaleSize: 3 },
                    itemStyle: { borderWidth: 2, borderColor: surface, borderRadius: 3 },
                    data: noData
                        ? [{ id: 'empty', name: '暂无数据', value: 1, itemStyle: { color: color('--hairline-strong') } }]
                        : [{ id: 'good', name: '合格', value: row.good, itemStyle: { color: good } },
                           { id: 'bad', name: '不良', value: row.bad, itemStyle: { color: bad } }]
                }]
            }, { replaceMerge: ['series'] });
        } else {
            const horizontal = element.clientWidth < 520;
            const categoryAxis = {
                type: 'category', data: rows.map(row => row.name),
                axisLine: { show: false }, axisTick: { show: false },
                axisLabel: {
                    color: muted, fontSize: 12, interval: 0, width: horizontal ? 108 : 105,
                    overflow: 'truncate', lineHeight: 18,
                    formatter: name => horizontal ? name : name.length > 8 ? name.slice(0, 7) + '\n' + name.slice(7) : name
                }
            };
            const valueAxis = {
                type: 'value', min: 0, minInterval: 1,
                axisLabel: { color: muted, fontSize: 11 },
                splitLine: { lineStyle: { color: color('--hairline'), type: 'dashed' } }
            };
            chart.setOption({
                ...common,
                grid: horizontal
                    ? { top: 15, right: 52, bottom: 24, left: 14, containLabel: true }
                    : { top: 30, right: 20, bottom: 16, left: 12, containLabel: true },
                xAxis: horizontal ? valueAxis : categoryAxis,
                yAxis: horizontal ? { ...categoryAxis, inverse: true } : valueAxis,
                series: [{
                    id: 'production', name: '今日产量', type: 'bar', barMaxWidth: 44,
                    data: rows.map(row => ({
                        id: row.id, name: row.name, value: row.total,
                        itemStyle: {
                            opacity: rows.some(item => item.selected) && !row.selected ? 0.45 : 1,
                            color: new echarts.graphic.LinearGradient(0, 0, horizontal ? 1 : 0, horizontal ? 0 : 1,
                                [{ offset: 0, color: accent }, { offset: 1, color: color('--chart-accent-deep') }]),
                            borderRadius: horizontal ? [0, 4, 4, 0] : [4, 4, 0, 0]
                        }
                    })),
                    label: { show: true, position: horizontal ? 'right' : 'top', color: ink, fontSize: 13 },
                    emphasis: { focus: 'self' }
                }]
            }, { replaceMerge: ['series', 'xAxis', 'yAxis'] });
        }
    }

    function resized() {
        cancelAnimationFrame(frame);
        frame = requestAnimationFrame(() => {
            if (!disposed && !document.hidden) {
                chart.resize();
                paint();
            }
        });
    }
    const observer = new ResizeObserver(resized);
    observer.observe(element);
    const themeObserver = new MutationObserver(paint);
    themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme', 'data-motion'] });
    const visibilityChanged = () => {
        if (document.hidden) chart.getZr().animation.stop();
        else { chart.getZr().animation.start(); resized(); }
    };
    document.addEventListener('visibilitychange', visibilityChanged);
    reducedMotion.addEventListener('change', paint);
    chart.on('click', params => {
        const row = kind === 'quality' ? rows[0] : rows[params.dataIndex];
        if (row && callback) callback.invokeMethodAsync('SelectDevice', row.id).catch(() => {});
    });

    return {
        update(data) { rows = data; paint(); },
        dispose() {
            disposed = true;
            cancelAnimationFrame(frame);
            observer.disconnect();
            themeObserver.disconnect();
            document.removeEventListener('visibilitychange', visibilityChanged);
            reducedMotion.removeEventListener('change', paint);
            chart.dispose();
        }
    };
}
