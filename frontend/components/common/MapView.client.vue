<template>
  <div class="relative w-full h-full min-h-[300px] rounded-2xl overflow-hidden border border-slate-200 shadow-sm">
    <div ref="mapContainer" class="w-full h-full min-h-[300px] z-10" />

    <!-- Current coordinates or helper overlay -->
    <div v-if="selectable" class="absolute bottom-3 left-3 z-20 bg-white/90 backdrop-blur-sm px-3 py-1.5 rounded-lg border border-slate-200 text-[11px] text-slate-600 font-medium shadow-sm pointer-events-none">
      <span>📍 Nhấp vào bản đồ để chọn tọa độ vị trí</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import L from 'leaflet'

export interface MapMarker {
  id: string
  name: string
  latitude: number
  longitude: number
  price?: number
  address?: string
}

const props = withDefaults(
  defineProps<{
    latitude?: number
    longitude?: number
    zoom?: number
    markers?: MapMarker[]
    selectable?: boolean
  }>(),
  {
    latitude: 21.0285, // Default Hanoi coordinates
    longitude: 105.8542,
    zoom: 13,
    markers: () => [],
    selectable: false,
  }
)

const emit = defineEmits<{
  (e: 'bounds-changed', bounds: { swLat: number; swLon: number; neLat: number; neLon: number }): void
  (e: 'select-location', loc: { latitude: number; longitude: number }): void
  (e: 'click-marker', id: string): void
}>()

const mapContainer = ref<HTMLElement | null>(null)
let map: L.Map | null = null
let markerGroup: L.LayerGroup | null = null
let selectedPin: L.Marker | null = null

const initMap = () => {
  if (!mapContainer.value) return

  map = L.map(mapContainer.value).setView([props.latitude, props.longitude], props.zoom)

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors',
    maxZoom: 19,
  }).addTo(map)

  markerGroup = L.layerGroup().addTo(map)

  // Bounds change listener
  map.on('moveend', () => {
    if (!map) return
    const bounds = map.getBounds()
    emit('bounds-changed', {
      swLat: bounds.getSouthWest().lat,
      swLon: bounds.getSouthWest().lng,
      neLat: bounds.getNorthEast().lat,
      neLon: bounds.getNorthEast().lng,
    })
  })

  // Location selection on click
  if (props.selectable) {
    map.on('click', (e: L.LeafletMouseEvent) => {
      const { lat, lng } = e.latlng
      emit('select-location', { latitude: lat, longitude: lng })

      if (selectedPin && map) {
        selectedPin.setLatLng([lat, lng])
      } else if (map) {
        selectedPin = L.marker([lat, lng]).addTo(map)
      }
    })
  }

  updateMarkers()
}

const updateMarkers = () => {
  if (!markerGroup || !map) return
  markerGroup.clearLayers()

  props.markers.forEach((m) => {
    if (typeof m.latitude !== 'number' || typeof m.longitude !== 'number') return

    const marker = L.marker([m.latitude, m.longitude])
    marker.bindPopup(`
      <div style="font-family: system-ui, sans-serif; padding: 4px;">
        <h4 style="font-weight: 700; font-size: 13px; margin: 0 0 4px 0; color: #0f172a;">${m.name}</h4>
        ${m.address ? `<p style="font-size: 11px; color: #64748b; margin: 0 0 4px 0;">${m.address}</p>` : ''}
        ${m.price ? `<p style="font-size: 12px; font-weight: 700; color: #059669; margin: 0;">${new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(m.price)}</p>` : ''}
      </div>
    `)

    marker.on('click', () => {
      emit('click-marker', m.id)
    })

    markerGroup?.addLayer(marker)
  })
}

watch(
  () => props.markers,
  () => {
    updateMarkers()
  },
  { deep: true }
)

watch(
  () => [props.latitude, props.longitude],
  ([newLat, newLng]) => {
    if (map && newLat && newLng) {
      map.setView([newLat, newLng], props.zoom)
    }
  }
)

onMounted(() => {
  initMap()
})

onBeforeUnmount(() => {
  if (map) {
    map.remove()
    map = null
  }
})
</script>
