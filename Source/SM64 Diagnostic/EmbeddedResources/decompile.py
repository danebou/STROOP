import dakompiler

b = loadBindings('Resources\DAKompiler\sm64 ram map.txt', 'J')
marioRam = RAMSnapshot('Resources\DAKompiler\marioRam',0x80000000)

# Return output to IronPython
decompileFunction(marioRam,b, 0x8026BFC8, args = ['A0 mario *Mario'])