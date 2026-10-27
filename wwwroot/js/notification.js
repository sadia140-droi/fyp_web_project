const NOTES_KEY = 'ck_notifications_v1';
function idGen() { return 'N' + Math.floor(Math.random() * 900000 + 100000) }
const sample = [
    { id: idGen(), title: 'New order received', body: '2x Biryani from Ali', time: Date.now() - 200000, status: 'unread', link: 'orders.html' },
    { id: idGen(), title: 'Payment received', body: 'Rs. 450 from Sara (JazzCash)', time: Date.now() - 500000, status: 'read', link: 'earnings.html' },
    { id: idGen(), title: 'Order delayed', body: 'Order #1027 delayed by 20 mins', time: Date.now() - 800000, status: 'unread', link: 'orders.html' }
];
let notes = [];
function loadNotes() { const raw = localStorage.getItem(NOTES_KEY); if (raw) { try { notes = JSON.parse(raw); } catch (e) { notes = sample.slice(); saveNotes(); } } else { notes = sample.slice(); saveNotes(); } }
function saveNotes() { localStorage.setItem(NOTES_KEY, JSON.stringify(notes)); renderNotes(); }

const notesList = document.getElementById('notesList');
const unreadCount = document.getElementById('unreadCount');
const btnAll = document.getElementById('btnAll');
const btnUnread = document.getElementById('btnUnread');
const btnClear = document.getElementById('btnClear');
const searchNote = document.getElementById('searchNote');
let filter = 'all';

function timeAgo(ms) { const s = Math.floor((Date.now() - ms) / 1000); if (s < 60) return s + 's ago'; if (s < 3600) return Math.floor(s / 60) + 'm ago'; if (s < 86400) return Math.floor(s / 3600) + 'h ago'; return Math.floor(s / 86400) + 'd ago'; }

function renderNotes() {
    notesList.innerHTML = ''; const q = (searchNote.value || '').toLowerCase(); let filtered = notes.slice().sort((a, b) => b.time - a.time);
    if (filter === 'unread') filtered = filtered.filter(n => n.status === 'unread');
    if (q) filtered = filtered.filter(n => (n.title + n.body).toLowerCase().includes(q));
    if (filtered.length === 0) { notesList.innerHTML = '<div style="text-align:center;padding:18px;color:#666">No notifications</div>'; }
    filtered.forEach(n => {
        const div = document.createElement('div'); div.className = 'note' + (n.status === 'unread' ? ' unread' : '');
        div.innerHTML = `<div class="dot" style="width:10px;height:10px;border-radius:50%;background:${n.status === 'unread' ? '#e67e00' : '#ccc'};margin-top:6px"></div>
         <div class="text"><strong>${n.title}</strong><div style="margin-top:6px;color:var(--muted)">${n.body}</div></div>
         <div style="text-align:right"><div class="time">${timeAgo(n.time)}</div><div class="actions" style="margin-top:8px">
           <button class="btn btn-mark" data-id="${n.id}" data-action="toggle">${n.status === 'unread' ? 'Mark read' : 'Mark unread'}</button>
           <a href="${n.link || '#'}" style="text-decoration:none"><button class="btn" data-id="${n.id}" data-action="open">Open</button></a>
           <button class="btn btn-del" data-id="${n.id}" data-action="del">Delete</button>
         </div></div>`;
        notesList.appendChild(div);
    });
    unreadCount.textContent = notes.filter(n => n.status === 'unread').length;
    // attach listeners
    document.querySelectorAll('.btn').forEach(b => {
        b.onclick = function (e) { const id = this.dataset.id; const action = this.dataset.action; if (action === 'toggle') { toggleRead(id); } else if (action === 'del') { deleteNote(id); } else if (action === 'open') { /* link handled by <a> */ } }
    });
}

function toggleRead(id) { const n = notes.find(x => x.id === id); if (!n) return; n.status = n.status === 'unread' ? 'read' : 'unread'; saveNotes(); }
function deleteNote(id) { if (!confirm('Delete this notification?')) return; notes = notes.filter(x => x.id !== id); saveNotes(); }
function clearAll() { if (!confirm('Clear all notifications?')) return; notes = []; saveNotes(); }

btnAll.onclick = () => { filter = 'all'; renderNotes(); }
btnUnread.onclick = () => { filter = 'unread'; renderNotes(); }
btnClear.onclick = () => { clearAll(); }
searchNote.addEventListener('input', () => renderNotes());

// init
loadNotes(); renderNotes();