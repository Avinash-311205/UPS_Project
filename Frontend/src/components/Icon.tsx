const iconPaths: Record<string, string> = {
  activity: 'M3 12h4l3-8 4 16 3-8h4',
  alert: 'M12 9v4m0 4h.01M10.3 3.9 2.8 17a2 2 0 0 0 1.7 3h15a2 2 0 0 0 1.7-3l-7.5-13.1a2 2 0 0 0-3.4 0Z',
  arrows: 'M17 3l4 4-4 4M3 7h18M7 21l-4-4 4-4m14 4H3',
  arrowUp: 'M7 14l5-5 5 5M12 19V9',
  bell: 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9m-8 12h4',
  check: 'm5 12 4 4L19 6',
  chevron: 'm9 18 6-6-6-6',
  clock: 'M12 8v4l3 2m6-2a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z',
  close: 'M18 6 6 18M6 6l12 12',
  grid: 'M3 3h8v8H3zM13 3h8v5h-8zM13 10h8v11h-8zM3 13h8v8H3z',
  layers: 'm12 3 9 5-9 5-9-5 9-5Zm-9 9 9 5 9-5M3 16l9 5 9-5',
  menu: 'M4 6h16M4 12h16M4 18h16',
  package: 'm12 3 9 5-9 5-9-5 9-5Zm-9 5v8l9 5 9-5V8m-9 5v8',
  pin: 'M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Zm-5 0a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z',
  refresh: 'M20 7v5h-5M4 17v-5h5m-4.1-3A8 8 0 0 1 18 6l2 2M4 16l2 2a8 8 0 0 0 13.1-3',
  search: 'm20 20-4.5-4.5M18 10.5a7.5 7.5 0 1 1-15 0 7.5 7.5 0 0 1 15 0Z',
  server: 'M4 4h16v6H4zM4 14h16v6H4zM8 7h.01M8 17h.01M12 7h4m-4 10h4',
  shield: 'M12 22s8-4 8-11V5l-8-3-8 3v6c0 7 8 11 8 11Zm-3-11 2 2 4-4',
  truck: 'M3 6h11v11H3zM14 10h4l3 3v4h-7zM7 20a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm11 0a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z',
  warehouse: 'M3 10 12 4l9 6v10H3V10Zm5 10v-6h8v6m-8-9h.01M12 11h.01m4-.01h.01',
  warning: 'M12 8v4m0 4h.01M10.3 3.9 2.8 17a2 2 0 0 0 1.7 3h15a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z',
}

export default function Icon({ name, size = 18, className = '' }: { name: string; size?: number; className?: string }) {
  return (
    <svg aria-hidden="true" className={className} width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
      <path d={iconPaths[name] ?? iconPaths.grid} />
    </svg>
  )
}
