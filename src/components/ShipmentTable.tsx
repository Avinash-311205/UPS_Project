import type { Shipment } from '../types'
import StatusPill from './StatusPill'

export default function ShipmentTable({ shipments }: { shipments: Shipment[] }) {
  return <div className="table-scroll"><table className="shipment-table"><thead><tr><th>SHIPMENT</th><th>DESTINATION</th><th>ETA</th><th>STATUS</th></tr></thead><tbody>{shipments.map((shipment) => <tr key={shipment.id}><td><strong className="shipment-id">{shipment.id}</strong><small>{shipment.customer}</small></td><td>{shipment.destination}</td><td>{shipment.eta}</td><td><StatusPill status={shipment.status} /></td></tr>)}</tbody></table></div>
}
