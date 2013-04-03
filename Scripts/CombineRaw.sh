#! /bin/bash
# Combine the multiple occurances of the same log file into one in an analysis directory.
# Should be run in the directory with the log files. Will create the directory "Analysis"
#    and create the combination files there.
# Invocation: CombineRaw.sh logDirectory

ANALYSISDIR="Analysis"

TEMP1=TEMP1-$$

mkdir -p ${ANALYSISDIR}

# Get the unique, undated prefixes from all the log file names
ls conn-*.log scene-*.log server-*.log | sed -e "s/-201...........\.log//" | sort -u > $TEMP1

# Create the combination files
cat $TEMP1 | while read filename ; do
    cat ${filename}-* > ${ANALYSISDIR}/${filename}.log
done

rm -f $TEMP1
