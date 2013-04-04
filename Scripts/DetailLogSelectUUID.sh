#! /bin/bash
# Read all the OpenSim log files in the current directory for references
#   to the passed UUID and output a cleaned up log file with only those
#   references sorted by time time written.
# Needs customizing for specific use.

UUID=$1
if [[ -z "$UUID" ]] ; then
	echo "Must specify a UUID to sort"
	echo "Invocation: $0 UUID"
	exit 1
fi

TEMP1=/tmp/TEMP1.$$
TEMP2=/tmp/TEMP2.$$
SEDSCRIPT=/tmp/SEDSCRIPT.$$
TEMPDIR=/tmp/DIR.$$
rm -f $TEMP1 $TEMP2 $SEDSCRIPT

cat > $SEDSCRIPT <<EOFFF
s/^log-//
s/-201...........\.log:/,/
s/ DEBUG -.*\\[/:[/
s/^persist/persist/
s/^clientmgr/climgr/
s/ clientmgr/ climgr/g
s/ obj=........-....-....-....-............//
s/\\[PRIM/    [PRIM/
EOFFF

rm -rf $TEMPDIR
mkdir -p $TEMPDIR

for file in log-*.log ; do
	grep -H $UUID $file |  sed -f $SEDSCRIPT > $TEMPDIR/$file
done

sort -n -t, -m --key=6,6 --key=4,4 $TEMPDIR/*

rm -rf $TEMPDIR

rm -f $TEMP1 $TEMP2 $SEDSCRIPT
