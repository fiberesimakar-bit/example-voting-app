
var express = require('express'),
    async = require('async'),
    { Pool } = require('pg'),
    cookieParser = require('cookie-parser'),
    path = require('path'),
    app = express(),
    server = require('http').Server(app),
    io = require('socket.io')(server);

var port = process.env.PORT || 4000;

io.on('connection', function (socket) {
  socket.emit('message', { text: 'Welcome!' });

  socket.on('subscribe', function (data) {
    socket.join(data.channel);
  });
});

var postgresPassword = process.env.POSTGRES_PASSWORD;

if (!postgresPassword) {
  throw new Error('POSTGRES_PASSWORD environment variable is missing.');
}

var pool = new Pool({
  host: 'db',
  port: 5432,
  user: 'postgres',
  password: postgresPassword,
  database: 'postgres'
});

async.retry(
  { times: 1000, interval: 1000 },
  function (callback) {
    pool.connect(function (err, client, done) {
      if (err) {
        console.error('Waiting for db');
        return callback(err);
      }

      // Release this connection; getVotes obtains its own connection below.
      done();
      callback(null);
    });
  },
  function (err) {
    if (err) {
      return console.error('Giving up connecting to db:', err.message);
    }

    console.log('Connected to db');

    getVotes();
  }
);

function getVotes() {
  pool.query(
    'SELECT vote, COUNT(id) AS count FROM votes GROUP BY vote',
    [],
    function (err, result) {
      if (err) {
        console.error('Error performing query:', err.message);
      } else {
        var votes = collectVotesFromResult(result);
        io.sockets.emit('scores', JSON.stringify(votes));
      }

      setTimeout(getVotes, 1000);
    }
  );
}

function collectVotesFromResult(result) {
  var votes = { a: 0, b: 0 };

  result.rows.forEach(function (row) {
    votes[row.vote] = parseInt(row.count, 10);
  });

  return votes;
}

app.use(cookieParser());
app.use(express.urlencoded({ extended: false }));
app.use(express.static(__dirname + '/views'));

app.get('/', function (req, res) {
  res.sendFile(path.resolve(__dirname, 'views', 'index.html'));
});

server.listen(port, function () {
  console.log('App running on port ' + server.address().port);
});
