function lineChart(canvasId, label, unit, data, url) {
    var ctx = document.getElementById(canvasId).getContext('2d');

    return new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => new Date(d.timestamp)).reverse(),
            datasets: [{
                label: label,
                data: data.map(d => d.value).reverse(),
                borderColor: 'rgba(12, 106, 86, 1)',
                borderWidth: 1,
                fill: false,
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    display: true,
                }
            },
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Time (t)'
                    },
                    ticks: {
                        display: false
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: label + (unit == "" ? "" : ` (${unit})`) 
                    },
                }
            }
        }
    });


}

function lineChartBinary(canvasId, label, unit, value1, value2, data, url) {
    var ctx = document.getElementById(canvasId).getContext('2d');

    return new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => new Date(d.timestamp)),
            datasets: [{
                label: label,
                data: data.map(d => d.value === value1 ? 1 : (d.value === value2 ? 2 : 0)),
                borderColor: 'rgba(12, 106, 86, 1)',
                borderWidth: 1,
                fill: false,
                stepped: true
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    display: true,
                }
            },
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Time (t)'
                    },
                    ticks: {
                        display: false
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: label + (unit == "" ? "" : ` (${unit})`)
                    },
                    ticks: {
                        stepSize: 1,
                        min: 1,
                        max: 2,
                        callback: function (value) {
                            if (value === 1) return value1;
                            if (value === 2) return value2;
                        }
                    }
                }
            }
        }
    });
}

async function fetchAndUpdate(url, temperatureChart, pressureChart, pumpSettingChart) {
    try {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`HTTP error. status: ${response.status}`);
        }

        const data = await response.json();

        // update "Status" section
        document.getElementById("currentTemperature").textContent = data.temperatureData[data.temperatureData.length - 1].value;        document.getElementById("currentPressure").textContent = data.pressureData[data.pressureData.length - 1].value;
        document.getElementById("currentPumpSetting").textContent = data.pumpSettingData[0].value;

        // update graphs
        temperatureChart.data.labels = data.temperatureData.map(d => new Date(d.timestamp)).reverse();
        temperatureChart.data.datasets[0].data = data.temperatureData.map(d => d.value).reverse();
        temperatureChart.update();

        pressureChart.data.labels = data.pressureData.map(d => new Date(d.timestamp)).reverse();
        pressureChart.data.datasets[0].data = data.pressureData.map(d => d.value).reverse();
        pressureChart.update();

        pumpSettingChart.data.labels = data.pumpSettingData.map(d => new Date(d.timestamp)).reverse();
        pumpSettingChart.data.datasets[0].data = data.pumpSettingData.map(d => d.value === "standard" ? 1 : (d.value === "speed" ? 2 : 0)).reverse();
        pumpSettingChart.update();

    } catch (err) {
        console.error("ERROR: ", err);
    }
}