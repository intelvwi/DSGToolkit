#! /bin/bash
# Reads all the log files in the current directory and outputs
#   a cleaned up log file sorted on the time written (second field).

TEMP1=/tmp/TEMP1.$$
TEMP2=/tmp/TEMP2.$$
SEDSCRIPT=/tmp/SEDSCRIPT.$$
TEMPDIR=/tmp/DIR.$$
rm -f $TEMP1 $TEMP2 $SEDSCRIPT

cat > $SEDSCRIPT <<EOFFF
s/log:201/log,201/
s/^log-//
s/^clientmgr/cltmgr/
s/,SendRelev,/, SendRelev,/
s/-r....-201.*\.log//
s/-r.....-201.*\.log//
s/-r......-201.*\.log//
EOFFF

rm -rf $TEMPDIR
mkdir -p $TEMPDIR

for file in log-*.log ; do
	perl -pe 'print "$ARGV:"' $file | sed -f "$SEDSCRIPT" > $TEMPDIR/$file
done

sort -t, -n -m --key=2,2 $TEMPDIR/*

rm -rf $TEMPDIR
rm -f $TEMP1 $TEMP2 $SEDSCRIPT
