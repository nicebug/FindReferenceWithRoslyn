# coding: utf-8
import sys

change_filename = {}


def main():
    filename = r"E:\DailyWork\WeSpeed\Code\PreDistribution\Client\UnityProj\1.txt"
    with open(filename, 'r') as f:
        for line in f:
            if line.startswith('+++'):
                # get changefile name list
                # +++ Assets/Scripts/ssGameWorld.cs (revision 190380)这种文件格式的处理
                key = line.split()[1].strip()
                if key not in change_filename:
                    change_filename[key] = []
            elif line.startswith('@@'):
                # 获得diff文件中变化的行数
                # @@ -1034,7 +1030,6 @@ 这种格式的处理
                num = line.split()[2].split(',')[0].split('+')[1].strip()
                change_filename[key].append(num)

        print change_filename


if __name__ == "__main__":
    main()
