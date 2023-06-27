import numpy as np
import matplotlib.pyplot as plt

def read_measurements(encryption_filename: str, decryption_filename: str, sizes_filename: str) -> tuple:
    with open(encryption_filename, 'r') as f:
        lines = [line.strip() for line in f.readlines()]
        lines = [line for line in lines if line != '']
        #print(lines)
        encryption_times = np.array([int(line) for line in lines])
    with open(decryption_filename, 'r') as f:
        lines = [line.strip() for line in f.readlines()]
        lines = [line for line in lines if line != '']
        #print(lines)
        decryption_times = np.array([int(line) for line in lines])
    with open(sizes_filename, 'r') as f:
        lines = [line.strip() for line in f.readlines()]
        lines = [line for line in lines if line != '']
        #print(lines)
        message_sizes = np.array([int(line) for line in lines])

    return encryption_times, decryption_times, message_sizes

def plot_measurements(encryption_times, decryption_times, message_sizes) -> None:
    plt.figure()
    plt.axes()
    plt.xlabel("Size of message (bytes) * 100")
    plt.ylabel("Time (ms)")
    listLegend = ['Encryption', 'Decryption']

    plt.title(f'Encryption and decryption duration')
    plt.plot(encryption_times)
    plt.plot(decryption_times)
    plt.legend(listLegend)
    plt.show()

def main():
    encryption_filename = './encryption_times.txt'
    decryption_filename = './decryption_times.txt'
    message_sizes_filename = './message_sizes.txt'

    enc_times, dec_times, message_sizes = read_measurements(encryption_filename=encryption_filename, decryption_filename=decryption_filename, sizes_filename=message_sizes_filename)
    plot_measurements(enc_times, dec_times, message_sizes)
    #print(message_sizes)
    #print(enc_times)
    #print(dec_times)


if __name__ == '__main__':
    main()