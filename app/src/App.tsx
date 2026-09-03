import { useState, useEffect, useRef } from 'react'
import { HubConnectionBuilder, HttpTransportType } from '@microsoft/signalr'

const isLocalhost = Boolean(
  window.location.hostname === 'localhost' ||
  window.location.hostname === '[::1]' || // IPv6 localhost
  window.location.hostname.match(/^127(?:\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}$/) // 127.0.0.1
);

const THEMES = [
  { id: 'arctic', label: 'Arctic', dot: '#1a56ff' },
  { id: 'obsidian', label: 'Obsidian', dot: '#a3ff47' },
  { id: 'terminal', label: 'Terminal', dot: '#39ff14' },
  { id: 'copper', label: 'Copper', dot: '#b5541a' },
  { id: 'frost', label: 'Frost', dot: '#00d4ff' },
  { id: 'crimson', label: 'Crimson', dot: '#e8173a' },
  { id: 'slate', label: 'Slate', dot: '#5b6af0' },
  { id: 'sand', label: 'Sand', dot: '#f5c842' },
  { id: 'violet', label: 'Violet', dot: '#b060ff' },
  { id: 'chalk', label: 'Chalk', dot: '#f2f2f0' },
  { id: 'aurora', label: 'Aurora', dot: '#00ffc8' },
  { id: 'paper', label: 'Paper', dot: '#1e1e18' },
]

interface Product {
  id: string
  name: string
  category: string
  description: string
  price: number
  stock: number
  specs: Record<string, string>
}

interface Order {
  orderId: string
  productId: string
  productName: string
  quantity: number
  totalPrice: number
  status: string
  createdAt: string
}


interface AuthState {
  token: string
  username: string
}

const FALLBACK_PRODUCTS: Product[] = [
  {
    id: 'p001',
    name: 'Dell Latitude 7440',
    category: 'Laptop',
    description: 'Business ultrabook with Intel vPro, enterprise security suite included.',
    price: 1349.00,
    stock: 0,
    specs: { CPU: 'Intel Core i7-1365U', RAM: '16 GB DDR5', Storage: '512 GB NVMe', Display: '14" FHD IPS' },
  },
  {
    id: 'p002',
    name: 'HP EliteDesk 800 G9',
    category: 'Desktop',
    description: 'Compact desktop tower optimized for enterprise workloads and remote management.',
    price: 879.00,
    stock: 0,
    specs: { CPU: 'Intel Core i5-12500', RAM: '32 GB DDR4', Storage: '1 TB SSD', GPU: 'Intel UHD 770' },
  },
  {
    id: 'p003',
    name: 'Cisco Catalyst 9200L',
    category: 'Networking',
    description: '24-port PoE+ managed switch with VLAN, QoS, and Cisco DNA subscription.',
    price: 2150.00,
    stock: 0,
    specs: { Ports: '24x GbE PoE+', Uplinks: '4x SFP+', PoE: '370 W budget', Management: 'Cisco DNA' },
  },
  {
    id: 'p004',
    name: 'LG 27UN850-W Monitor',
    category: 'Display',
    description: '4K IPS panel with USB-C 96W PD, ideal for hybrid workstations.',
    price: 499.00,
    stock: 0,
    specs: { Resolution: '3840×2160', Panel: 'IPS Nano', Refresh: '60 Hz', Connectivity: 'USB-C, HDMI, DP' },
  },
  {
    id: 'p005',
    name: 'Synology DS923+',
    category: 'Storage',
    description: '4-bay NAS for SMB, compatible with Synology hybrid cloud backup.',
    price: 599.00,
    stock: 0,
    specs: { Bays: '4x 3.5" SATA', CPU: 'AMD Ryzen R1600', RAM: '4 GB ECC', Network: '2x GbE' },
  },
  {
    id: 'p006',
    name: 'Logitech MX Keys S',
    category: 'Peripherals',
    description: 'Backlit wireless keyboard with smart actions and multi-device pairing.',
    price: 119.00,
    stock: 0,
    specs: { Connection: 'Bluetooth / Logi Bolt', Battery: '10 days backlit', Layout: 'Full-size', OS: 'Mac / Win' },
  },
]

export default function App() {
  const [theme, setTheme] = useState('arctic')
  const [themeOpen, setThemeOpen] = useState(false)
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [authState, setAuthState] = useState<AuthState | null>(null)
  const [showAuth, setShowAuth] = useState(false)
  const [showOrders, setShowOrders] = useState(false)
  const [orders, setOrders] = useState<Order[]>([])
  const [ordersLoading, setOrdersLoading] = useState(false)
  const [orderModal, setOrderModal] = useState<Product | null>(null)
  const [orderQty, setOrderQty] = useState(1)
  const [orderStatus, setOrderStatus] = useState<'idle' | 'placing' | 'success' | 'error'>('idle')
  const [authForm, setAuthForm] = useState({ username: '', password: '' })
  const [authLoading, setAuthLoading] = useState(false)
  const [authError, setAuthError] = useState('')
  const [successMsg, setSuccessMsg] = useState('')
  const [filterCategory, setFilterCategory] = useState('All')
  const themeRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme === 'arctic' ? '' : theme)
  }, [theme])

  useEffect(() => {
    const handleClick = (e: MouseEvent) => {
      if (themeRef.current && !themeRef.current.contains(e.target as Node)) setThemeOpen(false)
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [])

  useEffect(() => {
    const stored = sessionStorage.getItem('it_auth')
    if (stored) setAuthState(JSON.parse(stored))
  }, [])

  useEffect(() => {
    let disposed = false
    const connection = new HubConnectionBuilder()
      .withUrl('http://0.0.0.0:5255/hubs/inventory', {
        transport: HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .build()

    fetch('http://0.0.0.0:5255/sales/items')
      .then(response => response.json())
      .then(data => {
        if (!disposed) setProducts(Array.isArray(data) ? data : FALLBACK_PRODUCTS)
      })
      .catch(() => {
        if (!disposed) setProducts(FALLBACK_PRODUCTS)
      })
      .finally(() => {
        if (!disposed) setLoading(false)
      })

    connection.on('StockUpdated', (update: { productId: number; stock: number }) => {
      setProducts(current => current.map(product =>
        Number(product.id) === update.productId
          ? { ...product, stock: update.stock }
          : product,
      ))
    })

    connection.start().catch(error => console.error('Inventory live updates unavailable:', error))

    return () => {
      disposed = true
      connection.stop()
    }
  }, [])

  const categories = ['All', ...Array.from(new Set(products.map(p => p.category)))]
  const filtered = filterCategory === 'All' ? products : products.filter(p => p.category === filterCategory)


  //Auth
  async function handleLogin(e: React.FormEvent) {
    e.preventDefault()
    setAuthLoading(true)
    setAuthError('')
    try {
      const res = await fetch('/sales/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(authForm),
      })
      if (!res.ok) throw new Error('Invalid credentials')
      const data = await res.json()
      const auth: AuthState = { token: data.token, username: authForm.username }
      setAuthState(auth)
      sessionStorage.setItem('it_auth', JSON.stringify(auth))
      setShowAuth(false)
      setAuthForm({ username: '', password: '' })
    } catch {
      // demo mode: accept any non-empty credentials
      if (authForm.username && authForm.password) {
        const auth: AuthState = { token: 'demo_token_' + Date.now(), username: authForm.username }
        setAuthState(auth)
        sessionStorage.setItem('it_auth', JSON.stringify(auth))
        setShowAuth(false)
        setAuthForm({ username: '', password: '' })
      } else {
        setAuthError('Enter username and password.')
      }
    } finally {
      setAuthLoading(false)
    }
  }

  function handleLogout() {
    setAuthState(null)
    sessionStorage.removeItem('it_auth')
    setShowOrders(false)
    setOrders([])
  }

  async function fetchOrders() {
    if (!authState) return
    setOrdersLoading(true)
    try {
      const res = await fetch(isLocalhost? 'http://0.0.0.0:5255/sales/items': 'https://flashsale-syue.onrender.com/sales/items', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${authState.token}` },
        body: JSON.stringify({ username: authState.username }),
      })
      if (!res.ok) throw new Error()
      const data = await res.json()
      setOrders(Array.isArray(data) ? data : [])
    } catch {
      setOrders([])
    } finally {
      setOrdersLoading(false)
    }
  }

  function openOrders() {
    setShowOrders(true)
    fetchOrders()
  }

  async function placeOrder() {
    if (!orderModal) return
    setOrderStatus('placing')
    try {
      const res = await fetch('http://0.0.0.0:5255/sales/order', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(authState ? { Authorization: `Bearer ${authState.token}` } : {}),
        },
        body: JSON.stringify({userId: 1, productId: orderModal.id, quantity: orderQty, price: 400}),
      })
      if (!res.ok) throw new Error()
      setOrderStatus('success')
      setSuccessMsg(`Ordered ${orderQty}× ${orderModal.name}`)
      setTimeout(() => {
        setOrderModal(null)
        setOrderStatus('idle')
        setOrderQty(1)
      }, 1800)
    } catch {
      setOrderStatus('error')
      setTimeout(() => setOrderStatus('idle'), 2000)
    }
  }

  const currentTheme = THEMES.find(t => t.id === theme) ?? THEMES[0]

  return (
    <div style={{ minHeight: '100vh', backgroundColor: 'var(--bg)', color: 'var(--fg)' }}>
      {/* Header */}
      <header style={{ borderBottom: '1px solid var(--border)', position: 'sticky', top: 0, zIndex: 40, backdropFilter: 'blur(12px)', backgroundColor: 'var(--bg)', opacity: 0.97 }}>
        <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px', height: 56, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span className="mono" style={{ fontSize: 13, fontWeight: 600, letterSpacing: '0.08em', color: 'var(--accent)' }}>IT/STORE</span>
            <span style={{ width: 1, height: 16, background: 'var(--border)' }} />
            <span style={{ fontSize: 12, color: 'var(--muted)', fontWeight: 400 }}>Equipment Sales</span>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            {/* Theme switcher */}
            <div ref={themeRef} style={{ position: 'relative' }}>
              <button
                onClick={() => setThemeOpen(o => !o)}
                style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 12px', border: '1px solid var(--border)', borderRadius: 4, background: 'var(--card)', cursor: 'pointer', fontSize: 12, color: 'var(--fg)', fontFamily: 'inherit' }}
              >
                <span style={{ width: 8, height: 8, borderRadius: '50%', background: currentTheme.dot }} />
                <span>{currentTheme.label}</span>
                <span style={{ color: 'var(--muted)', marginLeft: 2 }}>▾</span>
              </button>
              {themeOpen && (
                <div style={{ position: 'absolute', right: 0, top: 'calc(100% + 6px)', background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 4, minWidth: 140, boxShadow: '0 8px 24px rgba(0,0,0,0.12)', zIndex: 100 }}>
                  {THEMES.map(t => (
                    <button
                      key={t.id}
                      onClick={() => { setTheme(t.id); setThemeOpen(false) }}
                      style={{ display: 'flex', alignItems: 'center', gap: 10, width: '100%', padding: '10px 14px', background: t.id === theme ? 'var(--tag)' : 'transparent', border: 'none', cursor: 'pointer', fontSize: 12, color: 'var(--fg)', fontFamily: 'inherit', textAlign: 'left' }}
                    >
                      <span style={{ width: 8, height: 8, borderRadius: '50%', background: t.dot, flexShrink: 0 }} />
                      {t.label}
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Auth */}
            {authState ? (
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <button
                  onClick={openOrders}
                  style={{ fontSize: 12, padding: '6px 12px', border: '1px solid var(--border)', borderRadius: 4, background: 'transparent', color: 'var(--fg)', cursor: 'pointer', fontFamily: 'inherit' }}
                >
                  My Orders
                </button>
                <span style={{ fontSize: 12, color: 'var(--muted)' }}>{authState.username}</span>
                <button
                  onClick={handleLogout}
                  style={{ fontSize: 12, padding: '6px 12px', border: 'none', background: 'transparent', color: 'var(--muted)', cursor: 'pointer', fontFamily: 'inherit' }}
                >
                  Sign out
                </button>
              </div>
            ) : (
              <button
                onClick={() => setShowAuth(true)}
                style={{ fontSize: 12, padding: '6px 14px', border: 'none', borderRadius: 4, background: 'var(--accent)', color: 'var(--accent-fg)', cursor: 'pointer', fontFamily: 'inherit', fontWeight: 500 }}
              >
                Sign in
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Main */}
      <main style={{ maxWidth: 1200, margin: '0 auto', padding: '40px 24px 80px' }}>
        {/* Page heading */}
        <div style={{ marginBottom: 36 }}>
          <h1 style={{ fontSize: 28, fontWeight: 600, letterSpacing: '-0.02em', marginBottom: 6 }}>Available Equipment</h1>
          <p style={{ fontSize: 13, color: 'var(--muted)' }}>{products.length} items in inventory</p>
        </div>

        {/* Category filter */}
        <div style={{ display: 'flex', gap: 8, marginBottom: 32, flexWrap: 'wrap' }}>
          {categories.map(cat => (
            <button
              key={cat}
              onClick={() => setFilterCategory(cat)}
              style={{
                fontSize: 11,
                fontWeight: 500,
                padding: '5px 12px',
                borderRadius: 2,
                border: '1px solid var(--border)',
                background: cat === filterCategory ? 'var(--accent)' : 'var(--tag)',
                color: cat === filterCategory ? 'var(--accent-fg)' : 'var(--tag-fg)',
                cursor: 'pointer',
                fontFamily: 'inherit',
                letterSpacing: '0.04em',
                transition: 'background 0.15s, color 0.15s',
              }}
            >
              {cat.toUpperCase()}
            </button>
          ))}
        </div>

        {/* Product grid */}
        {loading ? (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: 16 }}>
            {[...Array(6)].map((_, i) => (
              <div key={i} style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 4, padding: 24, height: 200, opacity: 0.4 }}>
                <div style={{ height: 12, background: 'var(--border)', borderRadius: 2, width: '60%', marginBottom: 12 }} />
                <div style={{ height: 10, background: 'var(--border)', borderRadius: 2, width: '40%', marginBottom: 24 }} />
                <div style={{ height: 10, background: 'var(--border)', borderRadius: 2, width: '80%', marginBottom: 8 }} />
                <div style={{ height: 10, background: 'var(--border)', borderRadius: 2, width: '70%' }} />
              </div>
            ))}
          </div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: 16 }}>
            {filtered.map(product => (
              <ProductCard key={product.id} product={product} onOrder={() => { setOrderModal(product); setOrderQty(1); setOrderStatus('idle') }} />
            ))}
          </div>
        )}
      </main>

      {/* Order Modal */}
      {orderModal && (
        <Overlay onClose={() => { setOrderModal(null); setOrderStatus('idle'); setOrderQty(1) }}>
          <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 6, padding: 32, width: '100%', maxWidth: 480 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 20 }}>
              <div>
                <span className="mono" style={{ fontSize: 10, color: 'var(--accent)', letterSpacing: '0.1em' }}>{orderModal.category.toUpperCase()}</span>
                <h2 style={{ fontSize: 18, fontWeight: 600, marginTop: 4 }}>{orderModal.name}</h2>
              </div>
              <button onClick={() => { setOrderModal(null); setOrderStatus('idle'); setOrderQty(1) }} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--muted)', fontSize: 18, lineHeight: 1 }}>×</button>
            </div>

            <p style={{ fontSize: 13, color: 'var(--muted)', marginBottom: 20, lineHeight: 1.6 }}>{orderModal.description}</p>

            <div style={{ background: 'var(--tag)', borderRadius: 4, padding: '12px 16px', marginBottom: 24 }}>
              {Object.entries(orderModal.specs).map(([k, v]) => (
                <div key={k} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, fontSize: 12 }}>
                  <span className="mono" style={{ color: 'var(--muted)' }}>{k}</span>
                  <span style={{ color: 'var(--fg)' }}>{v}</span>
                </div>
              ))}
            </div>

            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
              <div>
                <div className="mono" style={{ fontSize: 22, fontWeight: 600 }}>${(orderModal.price * orderQty).toLocaleString('en-US', { minimumFractionDigits: 2 })}</div>
                <div style={{ fontSize: 11, color: 'var(--muted)', marginTop: 2 }}>@ ${orderModal.price.toFixed(2)} each · {orderModal.stock} in stock</div>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <button onClick={() => setOrderQty(q => Math.max(1, q - 1))} style={{ width: 28, height: 28, border: '1px solid var(--border)', borderRadius: 2, background: 'var(--tag)', cursor: 'pointer', color: 'var(--fg)', fontSize: 16, display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'inherit' }}>−</button>
                <span className="mono" style={{ width: 24, textAlign: 'center', fontSize: 14 }}>{orderQty}</span>
                <button onClick={() => setOrderQty(q => Math.min(orderModal.stock, q + 1))} style={{ width: 28, height: 28, border: '1px solid var(--border)', borderRadius: 2, background: 'var(--tag)', cursor: 'pointer', color: 'var(--fg)', fontSize: 16, display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: 'inherit' }}>+</button>
              </div>
            </div>

            {orderStatus === 'success' && (
              <div style={{ padding: '10px 14px', background: 'var(--success)', color: 'var(--bg)', borderRadius: 3, fontSize: 13, marginBottom: 12, fontWeight: 500 }}>
                Order placed successfully.
              </div>
            )}
            {orderStatus === 'error' && (
              <div style={{ padding: '10px 14px', background: 'var(--danger)', color: '#fff', borderRadius: 3, fontSize: 13, marginBottom: 12 }}>
                Failed to place order. Please try again.
              </div>
            )}

            <button
              onClick={placeOrder}
              disabled={orderStatus === 'placing' || orderStatus === 'success'}
              style={{
                width: '100%',
                padding: '12px',
                background: orderStatus === 'success' ? 'var(--success)' : 'var(--accent)',
                color: 'var(--accent-fg)',
                border: 'none',
                borderRadius: 4,
                fontSize: 13,
                fontWeight: 600,
                cursor: orderStatus === 'placing' ? 'wait' : 'pointer',
                fontFamily: 'inherit',
                letterSpacing: '0.04em',
                opacity: orderStatus === 'placing' ? 0.7 : 1,
                transition: 'opacity 0.15s',
              }}
            >
              {orderStatus === 'placing' ? 'Placing order…' : orderStatus === 'success' ? 'Order confirmed' : 'Place order'}
            </button>

            {!authState && (
              <p style={{ fontSize: 11, color: 'var(--muted)', textAlign: 'center', marginTop: 12 }}>
                <button onClick={() => { setOrderModal(null); setShowAuth(true) }} style={{ background: 'none', border: 'none', color: 'var(--accent)', cursor: 'pointer', fontSize: 11, padding: 0, fontFamily: 'inherit' }}>Sign in</button> to track your orders
              </p>
            )}
          </div>
        </Overlay>
      )}

      {/* Auth Modal */}
      {showAuth && (
        <Overlay onClose={() => { setShowAuth(false); setAuthError('') }}>
          <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 6, padding: 32, width: '100%', maxWidth: 360 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
              <h2 style={{ fontSize: 16, fontWeight: 600 }}>Sign in</h2>
              <button onClick={() => { setShowAuth(false); setAuthError('') }} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--muted)', fontSize: 18, lineHeight: 1 }}>×</button>
            </div>
            <form onSubmit={handleLogin} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div>
                <label style={{ fontSize: 11, color: 'var(--muted)', display: 'block', marginBottom: 6, letterSpacing: '0.06em', fontWeight: 500 }}>USERNAME</label>
                <input
                  type="text"
                  value={authForm.username}
                  onChange={e => setAuthForm(f => ({ ...f, username: e.target.value }))}
                  style={{ width: '100%', padding: '9px 12px', border: '1px solid var(--border)', borderRadius: 3, background: 'var(--bg)', color: 'var(--fg)', fontSize: 13, fontFamily: 'inherit', outline: 'none', boxSizing: 'border-box' }}
                  autoFocus
                />
              </div>
              <div>
                <label style={{ fontSize: 11, color: 'var(--muted)', display: 'block', marginBottom: 6, letterSpacing: '0.06em', fontWeight: 500 }}>PASSWORD</label>
                <input
                  type="password"
                  value={authForm.password}
                  onChange={e => setAuthForm(f => ({ ...f, password: e.target.value }))}
                  style={{ width: '100%', padding: '9px 12px', border: '1px solid var(--border)', borderRadius: 3, background: 'var(--bg)', color: 'var(--fg)', fontSize: 13, fontFamily: 'inherit', outline: 'none', boxSizing: 'border-box' }}
                />
              </div>
              {authError && <p style={{ fontSize: 12, color: 'var(--danger)', margin: 0 }}>{authError}</p>}
              <button
                type="submit"
                disabled={authLoading}
                style={{ padding: '11px', background: 'var(--accent)', color: 'var(--accent-fg)', border: 'none', borderRadius: 4, fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit', marginTop: 4, opacity: authLoading ? 0.7 : 1 }}
              >
                {authLoading ? 'Signing in…' : 'Sign in'}
              </button>
            </form>
          </div>
        </Overlay>
      )}

      {/* Orders Panel */}
      {showOrders && authState && (
        <Overlay onClose={() => setShowOrders(false)}>
          <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 6, padding: 32, width: '100%', maxWidth: 600, maxHeight: '80vh', display: 'flex', flexDirection: 'column' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24, flexShrink: 0 }}>
              <div>
                <h2 style={{ fontSize: 16, fontWeight: 600 }}>My Orders</h2>
                <p style={{ fontSize: 12, color: 'var(--muted)', marginTop: 2 }}>{authState.username}</p>
              </div>
              <button onClick={() => setShowOrders(false)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--muted)', fontSize: 18, lineHeight: 1 }}>×</button>
            </div>

            <div style={{ overflowY: 'auto', flex: 1 }}>
              {ordersLoading ? (
                <div style={{ padding: '40px 0', textAlign: 'center', color: 'var(--muted)', fontSize: 13 }}>Loading orders…</div>
              ) : orders.length === 0 ? (
                <div style={{ padding: '40px 0', textAlign: 'center', color: 'var(--muted)', fontSize: 13 }}>No orders found.</div>
              ) : (
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border)' }}>
                      {['Order ID', 'Product', 'Qty', 'Total', 'Status', 'Date'].map(col => (
                        <th key={col} style={{ textAlign: 'left', padding: '0 8px 10px', fontSize: 10, color: 'var(--muted)', fontWeight: 600, letterSpacing: '0.08em', whiteSpace: 'nowrap' }}>{col.toUpperCase()}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {orders.map((order, i) => (
                      <tr key={order.orderId ?? i} style={{ borderBottom: '1px solid var(--border)' }}>
                        <td className="mono" style={{ padding: '12px 8px', fontSize: 11, color: 'var(--muted)' }}>{order.orderId}</td>
                        <td style={{ padding: '12px 8px', fontSize: 13 }}>{order.productName}</td>
                        <td className="mono" style={{ padding: '12px 8px', fontSize: 12, textAlign: 'right' }}>{order.quantity}</td>
                        <td className="mono" style={{ padding: '12px 8px', fontSize: 12 }}>${order.totalPrice?.toFixed(2)}</td>
                        <td style={{ padding: '12px 8px' }}>
                          <span style={{ fontSize: 10, padding: '3px 7px', borderRadius: 2, background: 'var(--tag)', color: 'var(--tag-fg)', fontWeight: 600, letterSpacing: '0.06em' }}>{(order.status ?? 'PENDING').toUpperCase()}</span>
                        </td>
                        <td className="mono" style={{ padding: '12px 8px', fontSize: 11, color: 'var(--muted)', whiteSpace: 'nowrap' }}>
                          {order.createdAt ? new Date(order.createdAt).toLocaleDateString() : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </Overlay>
      )}

      {/* Success toast */}
      {successMsg && (
        <div
          style={{ position: 'fixed', bottom: 24, right: 24, background: 'var(--accent)', color: 'var(--accent-fg)', padding: '12px 20px', borderRadius: 4, fontSize: 13, fontWeight: 500, zIndex: 200, boxShadow: '0 4px 16px rgba(0,0,0,0.2)' }}
          onAnimationEnd={() => setSuccessMsg('')}
        >
          {successMsg}
        </div>
      )}
    </div>
  )
}

function ProductCard({ product, onOrder }: { product: Product; onOrder: () => void }) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div
      style={{
        background: 'var(--card)',
        border: '1px solid var(--border)',
        borderRadius: 4,
        padding: 24,
        display: 'flex',
        flexDirection: 'column',
        gap: 0,
        transition: 'border-color 0.15s',
        cursor: 'default',
      }}
      onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--accent)')}
      onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--border)')}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 8 }}>
        <span className="mono" style={{ fontSize: 10, color: 'var(--accent)', letterSpacing: '0.1em', fontWeight: 500 }}>{product.category.toUpperCase()}</span>
        <span className="mono" style={{ fontSize: 10, color: product.stock < 5 ? 'var(--danger)' : 'var(--muted)' }}>{product.stock} in stock</span>
      </div>

      <h3 style={{ fontSize: 15, fontWeight: 600, marginBottom: 8, lineHeight: 1.3 }}>{product.name}</h3>
      <p style={{ fontSize: 12, color: 'var(--muted)', lineHeight: 1.6, marginBottom: 16, flex: 1 }}>{product.description}</p>

      {/* Specs (collapsed by default) */}
      <button
        onClick={() => setExpanded(e => !e)}
        style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0, fontSize: 11, color: 'var(--muted)', fontFamily: 'inherit', textAlign: 'left', marginBottom: expanded ? 10 : 0, display: 'flex', alignItems: 'center', gap: 4 }}
      >
        <span style={{ fontSize: 10, transition: 'transform 0.15s', display: 'inline-block', transform: expanded ? 'rotate(90deg)' : 'none' }}>▶</span>
        {expanded ? 'Hide specs' : 'View specs'}
      </button>

      {expanded && (
        <div style={{ background: 'var(--tag)', borderRadius: 3, padding: '10px 12px', marginBottom: 12 }}>
          {Object.entries(product.specs).map(([k, v]) => (
            <div key={k} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11, marginBottom: 4 }}>
              <span className="mono" style={{ color: 'var(--muted)' }}>{k}</span>
              <span>{v}</span>
            </div>
          ))}
        </div>
      )}

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 'auto', paddingTop: 16, borderTop: '1px solid var(--border)' }}>
        <span className="mono" style={{ fontSize: 18, fontWeight: 600 }}>${product.price.toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
        <button
          onClick={onOrder}
          style={{
            padding: '7px 16px',
            background: 'var(--accent)',
            color: 'var(--accent-fg)',
            border: 'none',
            borderRadius: 3,
            fontSize: 12,
            fontWeight: 600,
            cursor: 'pointer',
            fontFamily: 'inherit',
            letterSpacing: '0.04em',
          }}
        >
          Order
        </button>
      </div>
    </div>
  )
}

function Overlay({ children, onClose }: { children: React.ReactNode; onClose: () => void }) {
  return (
    <div
      onClick={e => { if (e.target === e.currentTarget) onClose() }}
      style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100, padding: 24 }}
    >
      {children}
    </div>
  )
}
