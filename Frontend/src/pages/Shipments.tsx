import type { Shipment, ShipmentStatus } from '../types'
import OperationsMap from '../components/OperationsMap'
import Icon from '../components/Icon'
import StatusPill from '../components/StatusPill'

export default function ShipmentsPage({ filteredShipments, search, setSearch, shipmentFilter, setShipmentFilter, continuityActive, canUpdate, showMap, onRecordUpdate }: {
  filteredShipments: Shipment[]
  search: string
  setSearch: (value: string) => void
  shipmentFilter: string
  setShipmentFilter: (value: string) => void
  continuityActive: boolean
  canUpdate: boolean
  showMap: boolean
  onRecordUpdate: (shipmentId: string, status: ShipmentStatus) => void
}) {
  const filters = ['All shipments', 'At risk', 'Continuity', 'On track']
  return <div className="page-stack"><section className="shipment-summary"><div className="shipment-summary-card"><span>ALL AFFECTED LOADS</span><strong>12</strong><small>Across 3 facilities</small></div><div className="shipment-summary-card"><span>AT RISK</span><strong className="risk-text">02</strong><small>Needs manual review</small></div><div className="shipment-summary-card"><span>CONTINUITY READY</span><strong className="continuity-text">08</strong><small>{continuityActive ? 'Backup workflows active' : 'Awaiting plan approval'}</small></div><div className="shipment-summary-card"><span>ON-TIME PROJECTED</span><strong>94<span className="percent-sign">%</span></strong><small>With continuity procedures</small></div></section>
    {showMap && <OperationsMap shipments={filteredShipments} hubOperator={canUpdate} />}
    <section className="panel full-shipments-panel"><div className="shipment-controls"><div><h2>Impacted shipments</h2><p>{canUpdate ? 'Only shipments assigned to your hub are shown. Update status after continuity is approved.' : 'Review loads affected by the EDI Gateway incident.'}</p></div><label className="search-field"><Icon name="search" size={16} /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search shipments..." aria-label="Search shipments" /></label></div><div className="filter-tabs" role="tablist" aria-label="Filter shipments">{filters.map((filter) => <button key={filter} role="tab" aria-selected={shipmentFilter === filter} className={shipmentFilter === filter ? 'filter-active' : ''} onClick={() => setShipmentFilter(filter)}>{filter}</button>)}</div><div className="full-table-wrap"><table className="shipment-table full-shipment-table"><thead><tr><th>SHIPMENT</th><th>CUSTOMER</th><th>DESTINATION</th><th>ETA</th><th>DEPENDENCY</th><th>STATUS</th>{canUpdate && <th>UPDATE STATUS</th>}</tr></thead><tbody>{filteredShipments.map((shipment) => <tr key={shipment.id}><td><strong className="shipment-id">{shipment.id}</strong></td><td>{shipment.customer}</td><td>{shipment.destination}</td><td>{shipment.eta}</td><td>{shipment.system}</td><td><StatusPill status={shipment.status} /></td>{canUpdate && <td><select className="shipment-status-select" aria-label={`Update status for ${shipment.id}`} value={shipment.status} disabled={!continuityActive} onChange={(event) => onRecordUpdate(shipment.id, event.target.value as ShipmentStatus)}><option>On track</option><option>At risk</option><option>Continuity</option></select></td>}</tr>)}</tbody></table>{filteredShipments.length === 0 && <div className="empty-state"><Icon name="search" size={24} /><strong>No shipments found</strong><span>Try another search or filter.</span></div>}</div><div className="panel-footnote"><span>Showing {filteredShipments.length} of {canUpdate ? '2 assigned' : '12 impacted'} shipment records</span><span>{continuityActive ? 'Continuity workflows active' : 'Continuity workflows pending approval'}</span></div></section></div>
}
