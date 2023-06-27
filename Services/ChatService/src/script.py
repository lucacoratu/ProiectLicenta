import numpy as np
import matplotlib.pyplot as plt

def read_measurements(encryption_filename: str, decryption_filename: str) -> tuple:
    with open(encryption_filename, 'r') as f:
        lines = [line.strip() for line in f.readlines()]
        encryption_metrics = [line for line in lines if line != '']

    with open(decryption_filename, 'r') as f:
        lines = [line.strip() for line in f.readlines()]
        decryption_metrics = [line for line in lines if line != '']

    return encryption_metrics, decryption_metrics

def parse_metrics(encryption_metrics: list[str], decryption_metrics: list[str]) -> dict:
    parsed_metrics = {}
    for metric in encryption_metrics:
        device_info, message_size, duration = metric.split('\t')
        if device_info not in parsed_metrics:
            parsed_metrics[device_info] = {'encryption': [], 'decryption': []}
        parsed_metrics[device_info]['encryption'].append({'size': int(message_size), 'duration': int(duration)})

    for metric in decryption_metrics:
        device_info, message_size, duration = metric.split('\t')
        if device_info not in parsed_metrics:
            parsed_metrics[device_info] = {'encryption': [], 'decryption': []}
        parsed_metrics[device_info]['decryption'].append({'size': int(message_size), 'duration': int(duration)})
    #print(parsed_metrics)
    return parsed_metrics

def plot_metrics(parsed_metrics: dict) -> None:
    #Plot the metrics for each device individualy
    number_devices = len(parsed_metrics)
    number_plots = number_devices * 2
    plt.title('Measurements')
    for index, device in enumerate(parsed_metrics):
        encryption_metrics, decryption_metrics = parsed_metrics[device]['encryption'], parsed_metrics[device]['decryption']
        encryption_metrics = sorted(encryption_metrics, key=lambda d: d['size'])
        print(encryption_metrics)
        decryption_metrics = sorted(decryption_metrics, key=lambda d: d['size'])
        message_sizes_encryption, elapsed_times_encryption = [metric['size'] for metric in encryption_metrics], [metric['duration'] for metric in encryption_metrics]
        message_sizes_decryption, elapsed_times_decryption = [metric['size'] for metric in decryption_metrics], [metric['duration'] for metric in decryption_metrics]
        plt.subplot(number_devices, 2, (index * 2) + 1)
        plt.xlabel("Message size (bytes)")
        plt.ylabel("Time (ms)")
        plt.scatter(message_sizes_encryption, elapsed_times_encryption)
        plt.title(device + " - Encryption")
        plt.subplot(number_devices, 2, (index * 2) + 1 + 1)
        plt.xlabel("Message size (bytes)")
        plt.ylabel("Time (ms)")
        plt.title(device + " - Decryption")
        plt.scatter(message_sizes_decryption, elapsed_times_decryption)
    plt.show()


def main():
    encryption_filename = './encryption_metrics.txt'
    decryption_filename = './decryption_metrics.txt'
    encryption_metrics, decryption_metrics = read_measurements(encryption_filename=encryption_filename, decryption_filename=decryption_filename)
    parsed_metrics = parse_metrics(encryption_metrics=encryption_metrics, decryption_metrics=decryption_metrics)
    plot_metrics(parsed_metrics=parsed_metrics)

if __name__ == '__main__':
    main()