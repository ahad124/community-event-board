import http from 'http';

const PORT = 5050;

const events = [
  {
    id: "1",
    title: "Tech Conference 2026",
    date: "2026-09-15T09:00:00.000Z",
    location: "San Francisco, CA",
    category: "Technology",
    description: "Join us for the premier technology conference of 2026, featuring keynote speeches from industry leaders, interactive workshops, and networking opportunities with developers worldwide.",
    organizer: "Tech Global Forum",
    capacity: 500,
    price: 299,
    agenda: [
      { time: "09:00 AM", topic: "Opening Keynote", speaker: "Jane Doe, CEO Tech Corp" },
      { time: "10:30 AM", topic: "Future of AI and Web Dev", speaker: "John Smith, Principal Engineer" }
    ]
  },
  {
    id: "2",
    title: "Sunset Music Festival",
    date: "2026-10-05T17:00:00.000Z",
    location: "Miami, FL",
    category: "Music",
    description: "Experience live performances from top indie artists and electronic musicians as the sun sets over Miami Beach. Includes food trucks, art installations, and a beachside lounge.",
    organizer: "Sunset Live Events",
    capacity: 2000,
    price: "Free",
    agenda: [
      { time: "05:00 PM", topic: "Gates Open & Warm-up Set", speaker: "DJ Breeze" },
      { time: "08:00 PM", topic: "Headliner Performance", speaker: "The Echoes" }
    ]
  },
  {
    id: "3",
    title: "Art & Design Expo",
    date: "2026-11-12T10:00:00.000Z",
    location: "New York, NY",
    category: "Design",
    description: "Explore the latest trends in digital design, typography, and contemporary fine art. Meet local creators, attend panel discussions, and shop limited-edition prints.",
    organizer: "NY Creative Guild",
    capacity: 300,
    price: 15,
    agenda: [
      { time: "10:00 AM", topic: "Panel: Design in the AI Era", speaker: "Moderator: Alice Wong" },
      { time: "02:00 PM", topic: "Typography Workshop", speaker: "Mark Davis, Typographer" }
    ]
  }
];

const server = http.createServer((req, res) => {
  // Add CORS headers so we can access it directly if needed
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  const url = new URL(req.url, `http://localhost:${PORT}`);

  if (url.pathname === '/events' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(events));
  } else if (url.pathname.startsWith('/events/') && req.method === 'GET') {
    const id = url.pathname.split('/')[2];
    const event = events.find(e => e.id === id);
    if (event) {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify(event));
    } else {
      res.writeHead(404, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ error: 'Event not found' }));
    }
  } else {
    res.writeHead(404, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ error: 'Not found' }));
  }
});

server.listen(PORT, () => {
  console.log(`Mock API server is running on http://localhost:${PORT}`);
});
