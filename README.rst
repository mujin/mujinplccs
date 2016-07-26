mujinplccs
==========

.. image:: https://travis-ci.org/mujin/mujinplccs.svg?branch=master
    :target: https://travis-ci.org/mujin/mujinplccs

Building
--------

To build on linux you will need `mono` and `nuget`. Follow the instruction here:
http://www.mono-project.com/docs/getting-started/install/linux/

::

  apt-get install mono nuget

To build, go into the project root directory:

::

  nuget restore mujinplccs.sln
  xbuild /tv:12.0 /p:Configuration=Release mujinplccs.sln

Running
-------

To run the examples:

::

  ./samples/mujinplcexamplecs/bin/Release/mujinplcexamplecs.exe

To connect from ipython:

::

  In [1]: import zmq; ctx = zmq.Context(); socket = ctx.socket(zmq.REQ); socket.connect("tcp://127.0.0.1:5555")

  In [2]: socket.send_json({"command": "read", "keys": ["startOrderCycle", "stopOrderCycle"]}); socket.recv_json()
  Out[2]: {u'keyvalues': {}}

  In [3]: socket.send_json({"command": "write", "keyvalues": {"startOrderCycle": True}}); socket.recv_json()
  Out[3]: {}

  In [4]: socket.send_json({"command": "read", "keys": ["startOrderCycle", "stopOrderCycle"]}); socket.recv_json()
  Out[4]: {u'keyvalues': {u'startOrderCycle': True}}

Testing
-------

To run the unit tests:

::

  mono ./packages/xunit.runner.console.2.1.0/tools/xunit.console.x86.exe ./tests/mujintestplccs/bin/Release/mujintestplccs.dll -verbose

  
