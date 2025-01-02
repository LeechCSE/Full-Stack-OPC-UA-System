# OPC UA Simulation Server
###### The OPC UA server simulation code used in this project is based on [opc-ua-sensor-simulator](https://github.com/flopach/opc-ua-sensor-simulator). Please refer to that repository for additional details on how to set up and run the server.
This is an OPC UA simulation server written in Python, which sends out 3 values from a real data set when it gets started.

## Sensor Data

When executed, the OPC UA server provides 3 values to the connected clients (infinite loop):

* Pump Temperature (in Fahrenheit), Float
* Pump Pressure (in bar), Float
* Pump Setting (standard or speed), String 

The data set [Pump sensor data](https://www.kaggle.com/nphantawee/pump-sensor-data) which is available on Kaggle is being used.
