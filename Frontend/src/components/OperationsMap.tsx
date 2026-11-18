import { Fragment, useState } from 'react'
import { divIcon } from 'leaflet'
import type { DivIcon, LatLngTuple } from 'leaflet'
import { MapContainer, Marker, Polyline, Popup, TileLayer, Circle } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import type { Shipment } from '../types'
import Icon from './Icon'

const hubPosition: LatLngTuple = [41.8781, -87.6298]

const routePositions: Record<string, { vehicle: LatLngTuple; destination: LatLngTuple; vehicleId: string }> = {
  'SHP-28491': { vehicle: [41.885, -87.755], destination: [41.8781, -87.6298], vehicleId: 'V-214' },
  'SHP-28487': { vehicle: [42.315, -87.84], destination: [43.0389, -87.9065], vehicleId: 'V-087' },
  'SHP-28476': { vehicle: [40.72, -87.24], destination: [39.7684, -86.1581], vehicleId: 'V-176' },
  'SHP-28462': { vehicle: [40.55, -84.15], destination: [39.9612, -82.9988], vehicleId: 'V-062' },
  'SHP-28451': { vehicle: [41.62, -85.12], destination: [42.3314, -83.0458], vehicleId: 'V-051' },
}

const routeColor = (status: Shipment['status']) => status === 'At risk' ? '#d8755b' : status === 'Continuity' ? '#278b73' : '#5579b8'

const vehicleIcon: DivIcon = divIcon({
  className: 'fleet-map-marker-shell',
  html: '<span class="fleet-map-marker"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h11v11H3zM14 10h4l3 3v4h-7zM7 20a2 2 0 1 0 0-4 2 2 0 0 0 0 4Zm11 0a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z"/></svg></span>',
  iconSize: [32, 32],
  iconAnchor: [16, 16],
})

const destinationIcons: Record<Shipment['status'], DivIcon> = {
  'On track': divIcon({ className: 'destination-marker-shell', html: '<span class="destination-marker destination-on-track"></span>', iconSize: [18, 18], iconAnchor: [9, 9] }),
  'At risk': divIcon({ className: 'destination-marker-shell', html: '<span class="destination-marker destination-at-risk"></span>', iconSize: [18, 18], iconAnchor: [9, 9] }),
  Continuity: divIcon({ className: 'destination-marker-shell', html: '<span class="destination-marker destination-continuity"></span>', iconSize: [18, 18], iconAnchor: [9, 9] }),
}

export default function OperationsMap({ shipments, hubOperator }: { shipments: Shipment[]; hubOperator: boolean }) {
  const [selectedId, setSelectedId] = useState(shipments[0]?.id ?? '')
  const selectedShipment = shipments.find((shipment) => shipment.id === selectedId) ?? shipments[0]

  return (
    <section className="operations-map-panel" aria-label="Fleet and route map">
      <div className="operations-map-heading">
        <div>
          <div className="operations-map-kicker"><span className="live-dot" /> CONTINUITY FLEET VIEW</div>
          <h2>{hubOperator ? 'Assigned vehicles & routes' : 'Hub delivery area'}</h2>
          <p>{hubOperator ? 'Your assigned movements and delivery instructions.' : 'Vehicle locations, impacted shipments, and operating-area disruptions.'}</p>
        </div>
        <div className="map-live-status"><span className="map-live-pulse" /> OpenStreetMap · demo positions</div>
      </div>

      <div className="operations-map-layout">
        <div className="operations-map-canvas">
          <MapContainer
            center={[41.2, -85.7]}
            zoom={6}
            minZoom={5}
            maxZoom={16}
            scrollWheelZoom={false}
            className="real-operations-map"
          >
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            {shipments.some((shipment) => shipment.status === 'At risk') && (
              <Circle center={hubPosition} radius={12000} pathOptions={{ color: '#d8755b', fillColor: '#f1a187', fillOpacity: 0.13, weight: 2, dashArray: '5 6' }}>
                <Popup>Operational disruption watch · Chicago hub area</Popup>
              </Circle>
            )}
            {shipments.map((shipment) => {
              const route = routePositions[shipment.id]
              if (!route) return null
              const active = shipment.id === selectedShipment?.id
              return (
                <Fragment key={shipment.id}>
                  <Polyline
                    positions={[hubPosition, route.vehicle, route.destination]}
                    pathOptions={{ color: routeColor(shipment.status), weight: active ? 5 : 3, opacity: active ? 0.95 : 0.7, dashArray: '8 7' }}
                    eventHandlers={{ click: () => setSelectedId(shipment.id) }}
                  >
                    <Popup>
                      <strong>{shipment.id}</strong><br />
                      Dispatch route to {shipment.destination}<br />
                      Status: {shipment.status}
                    </Popup>
                  </Polyline>
                  <Marker position={route.vehicle} icon={vehicleIcon} eventHandlers={{ click: () => setSelectedId(shipment.id) }}>
                    <Popup>
                      <strong>{route.vehicleId}</strong><br />
                      Assigned to {shipment.id}<br />
                      Demo position · not live GPS
                    </Popup>
                  </Marker>
                  <Marker position={route.destination} icon={destinationIcons[shipment.status]} eventHandlers={{ click: () => setSelectedId(shipment.id) }}>
                    <Popup>
                      <strong>{shipment.destination}</strong><br />
                      {shipment.id} · ETA {shipment.eta}
                    </Popup>
                  </Marker>
                </Fragment>
              )
            })}
            <Marker position={hubPosition}>
              <Popup>Northwind Logistics · Chicago hub</Popup>
            </Marker>
          </MapContainer>
          <div className="map-legend"><span><i className="legend-vehicle" /> Vehicle</span><span><i className="legend-route" /> Dispatch route</span><span><i className="legend-disruption" /> Disruption area</span></div>
          <div className="map-note"><Icon name="warning" size={14} /> {hubOperator ? 'Use assigned routes; confirm movement updates in the shipment table.' : 'Disruption watch: EDI Gateway impact across the Chicago operating area.'} Vehicle locations are illustrative, not live GPS.</div>
        </div>

        <aside className="map-route-list">
          <div className="map-route-list-heading"><strong>{hubOperator ? 'Assigned movements' : 'Active movements'}</strong><span>{shipments.length} routes</span></div>
          <div className="map-route-items">
            {shipments.map((shipment) => {
              const route = routePositions[shipment.id]
              if (!route) return null
              return (
                <button key={shipment.id} type="button" className={`map-route-item ${selectedShipment?.id === shipment.id ? 'selected' : ''}`} onClick={() => setSelectedId(shipment.id)}>
                  <span className="map-route-bullet" style={{ backgroundColor: routeColor(shipment.status) }}><Icon name="truck" size={14} /></span>
                  <span className="map-route-copy"><strong>{shipment.destination}</strong><small>{shipment.id} · {route.vehicleId}</small></span>
                  <span className={`map-route-status ${shipment.status === 'At risk' ? 'route-status-risk' : ''}`}>{shipment.status}</span>
                </button>
              )
            })}
          </div>
          {selectedShipment && (
            <div className="map-selected-route">
              <span className="card-kicker">SELECTED SHIPMENT</span>
              <strong>{selectedShipment.id}</strong>
              <span><Icon name="pin" size={14} /> {selectedShipment.destination}</span>
              <span><Icon name="clock" size={14} /> ETA {selectedShipment.eta}</span>
              <p>{hubOperator ? 'Follow the assigned route and record status changes in the shipment list.' : 'Review vehicle position and route impact during continuity operations.'}</p>
            </div>
          )}
        </aside>
      </div>
    </section>
  )
}
