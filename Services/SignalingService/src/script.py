import base64
import json
import matplotlib.pyplot as plt
import numpy as np

def read_measurements(filename: str) -> list:
    with open('measurements.csv', 'r') as f:
        lines = [line.strip() for line in f.readlines()]
    lines = [line for line in lines if line != '']
    return lines

def measurement_to_object(measurements: list) -> list:
    returnList = []
    for measurement in measurements:
        decoded = base64.b64decode(measurement)
        returnList.append(json.loads(decoded.decode()))
    return returnList

def categorize_measurement_objects(measurementObjects: list) -> dict:
    uniqueDevices = {}
    for obj in measurementObjects:
        if obj['deviceInfo'] not in uniqueDevices:
            uniqueDevices[obj['deviceInfo']] = {}
        if len(obj['frameRates']) not in uniqueDevices[obj['deviceInfo']]:
            uniqueDevices[obj['deviceInfo']][len(obj['frameRates'])] = []
        uniqueDevices[obj['deviceInfo']][len(obj['frameRates'])].append(obj['frameRates'])
    return uniqueDevices

def plot_measurements(categorized: dict) -> None:
    for _, device in enumerate(categorized):
        for numberParticipants in categorized[device]:
            values = np.empty((len(categorized[device][numberParticipants]),numberParticipants))
            for line, frameRates in enumerate(categorized[device][numberParticipants]):
                for column, frameRate in enumerate(frameRates):
                    values[line][column] = frameRate
            #values = np.transpose(values)
            #row = values[1,:]
            plt.figure()
            plt.axes()
            plt.xlabel("Measurement")
            plt.ylabel("Frame Rate (Frames / sec)")
            listLegend = ['Local']
            for index in range(1, numberParticipants):
                listLegend.append(f'Remote')
            plt.title(f'{device} - {numberParticipants} participants')
            for row in np.transpose(values):
                plt.plot(row)
            plt.legend(listLegend)
            plt.show()

def main():
    filename = 'measurements.csv'
    measurements = read_measurements(filename=filename)
    objects = measurement_to_object(measurements=measurements)
    categorized = categorize_measurement_objects(objects)
    plot_measurements(categorized)

if __name__ == '__main__':
    main()