/**
 * TMS Enterprise - Single Page Application Client
 * Modern JavaScript controlling API operations, SignalR real-time notifications,
 * interactive course management, student records, async transcript generation, and telemetry.
 */

// Application State
const state = {
  token: localStorage.getItem('tms_token') || null,
  refreshToken: localStorage.getItem('tms_refresh_token') || null,
  user: JSON.parse(localStorage.getItem('tms_user') || 'null'),
  theme: localStorage.getItem('tms_theme') || 'dark',
  activeTab: 'tab-courses',
  signalRConnection: null,
  courses: [],
  students: [],
  pollTimer: null
};

// DOM Content Loaded Initializer
document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  initTabs();
  initAuth();
  initSignalR();
  initHealthMonitor();
  initEventListeners();

  // Load initial data
  loadCourses();
  loadStudents();
  loadGrades();
  loadCertificates();
});

/* ----------------------------------------------------
   1. Theme & UI Interactions
   ---------------------------------------------------- */
function initTheme() {
  document.documentElement.setAttribute('data-theme', state.theme);
  updateThemeIcon();
}

function updateThemeIcon() {
  const icon = document.querySelector('#theme-toggle-btn i');
  if (!icon) return;
  if (state.theme === 'light') {
    icon.className = 'fa-solid fa-sun';
  } else {
    icon.className = 'fa-solid fa-moon';
  }
}

function toggleTheme() {
  state.theme = state.theme === 'dark' ? 'light' : 'dark';
  localStorage.setItem('tms_theme', state.theme);
  document.documentElement.setAttribute('data-theme', state.theme);
  updateThemeIcon();
}

function initTabs() {
  const tabButtons = document.querySelectorAll('.tab-btn');
  tabButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetId = btn.getAttribute('data-target');
      switchTab(targetId);
    });
  });
}

function switchTab(targetId) {
  state.activeTab = targetId;
  document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
  document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));

  const activeBtn = document.querySelector(`.tab-btn[data-target="${targetId}"]`);
  const activePanel = document.getElementById(targetId);

  if (activeBtn) activeBtn.classList.add('active');
  if (activePanel) activePanel.classList.add('active');

  // Trigger contextual data refresh if needed
  if (targetId === 'tab-courses') loadCourses();
  if (targetId === 'tab-students') loadStudents();
  if (targetId === 'tab-assessments') loadGrades();
  if (targetId === 'tab-transcripts') loadCertificates();
}

/* ----------------------------------------------------
   2. HTTP Client with Correlation ID & Telemetry
   ---------------------------------------------------- */
async function apiFetch(url, options = {}) {
  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {})
  };

  if (state.token) {
    headers['Authorization'] = `Bearer ${state.token}`;
  }

  // Generate correlation ID if not present
  if (!headers['X-Correlation-Id']) {
    headers['X-Correlation-Id'] = 'ui-' + Math.random().toString(36).substring(2, 9);
  }

  const startTime = performance.now();

  try {
    const response = await fetch(url, {
      ...options,
      headers
    });
    const endTime = performance.now();
    const duration = Math.round(endTime - startTime);

    const correlationId = response.headers.get('X-Correlation-Id') || headers['X-Correlation-Id'];
    const deprecation = response.headers.get('Deprecation') || response.headers.get('Depreciation') || 'false';

    let data = null;
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      data = await response.json();
    } else {
      data = await response.text();
    }

    return {
      ok: response.ok,
      status: response.status,
      statusText: response.statusText,
      duration,
      correlationId,
      deprecation,
      headers: response.headers,
      data
    };
  } catch (error) {
    const endTime = performance.now();
    return {
      ok: false,
      status: 0,
      statusText: 'Network Error',
      duration: Math.round(endTime - startTime),
      correlationId: headers['X-Correlation-Id'],
      deprecation: 'false',
      headers: new Headers(),
      data: { error: error.message }
    };
  }
}

/* ----------------------------------------------------
   3. Real-Time SignalR Hub Client
   ---------------------------------------------------- */
function initSignalR() {
  try {
    state.signalRConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/tms')
      .withAutomaticReconnect()
      .build();

    state.signalRConnection.on('ReceiveEnrollmentStatusUpdated', (enrollmentId, status) => {
      addActivityFeedItem('Enrollment Updated', `Enrollment #${enrollmentId} status updated to: ${status}`, 'fa-user-check');
      showToast(`Enrollment #${enrollmentId} status is now ${status}`, 'info');
      loadCourses();
    });

    state.signalRConnection.on('ReceiveTranscriptReady', (reportId, downloadUrl) => {
      addActivityFeedItem('Transcript Generated', `Transcript #${reportId} is ready for download`, 'fa-file-pdf');
      showToast(`Transcript #${reportId} is ready!`, 'success');
      
      const downloadBtn = document.getElementById('job-download-link');
      if (downloadBtn) {
        downloadBtn.href = downloadUrl;
        downloadBtn.classList.remove('hidden');
      }
      const jobBadge = document.getElementById('job-percent-badge');
      if (jobBadge) {
        jobBadge.textContent = 'Completed (100%)';
        jobBadge.className = 'badge badge-success';
      }
      const fill = document.getElementById('job-progress-fill');
      if (fill) fill.style.width = '100%';
    });

    state.signalRConnection.start()
      .then(() => {
        document.getElementById('ws-status-text').textContent = 'SignalR Active';
        addActivityFeedItem('SignalR Hub', 'Connected to real-time notification socket', 'fa-satellite-dish');
      })
      .catch(err => {
        console.warn('SignalR connection pending:', err);
        document.getElementById('ws-status-text').textContent = 'SignalR Reconnecting';
      });

  } catch (ex) {
    console.warn('SignalR not initialized:', ex);
  }
}

function addActivityFeedItem(title, text, iconClass = 'fa-info-circle') {
  const feed = document.getElementById('live-activity-feed');
  if (!feed) return;

  const item = document.createElement('div');
  item.className = 'feed-item';
  item.innerHTML = `
    <div class="feed-icon"><i class="fa-solid ${iconClass}"></i></div>
    <div class="feed-content">
      <p class="feed-title">${escapeHtml(title)}</p>
      <p class="feed-text" style="font-size: 0.8rem; color: var(--text-secondary);">${escapeHtml(text)}</p>
      <span class="feed-time">${new Date().toLocaleTimeString()}</span>
    </div>
  `;
  feed.prepend(item);
}

/* ----------------------------------------------------
   4. Health Monitor
   ---------------------------------------------------- */
async function initHealthMonitor() {
  checkHealth();
  setInterval(checkHealth, 15000);
}

async function checkHealth() {
  const res = await apiFetch('/health/live');
  const pill = document.getElementById('server-status-pill');
  const text = document.getElementById('server-status-text');
  const healthBadge = document.getElementById('health-status-badge');
  const pingText = document.getElementById('last-ping-time');

  if (res.ok) {
    if (pill) pill.innerHTML = `<span class="status-dot pulse-green"></span><span>Online (${res.duration}ms)</span>`;
    if (healthBadge) healthBadge.textContent = '100% Operational';
    if (pingText) pingText.textContent = `Response: ${res.duration}ms`;
  } else {
    if (pill) pill.innerHTML = `<span class="status-dot" style="background: var(--color-rose);"></span><span>Degraded</span>`;
    if (healthBadge) {
      healthBadge.textContent = 'Service Degraded';
      healthBadge.className = 'stat-value text-rose';
    }
  }
}

/* ----------------------------------------------------
   5. Course Management Module
   ---------------------------------------------------- */
async function loadCourses() {
  const container = document.getElementById('courses-container');
  const res = await apiFetch('/api/v2/courses');

  if (!res.ok) {
    container.innerHTML = `<div class="text-center py-4 text-secondary">Unable to load courses. Please check connection.</div>`;
    return;
  }

  // Handle shaped response or items list
  const items = res.data?.Data || res.data?.Items || res.data?.items || [];
  state.courses = items;

  document.getElementById('count-courses').textContent = items.length;
  renderCoursesGrid(items);
  updateCourseSelects(items);
}

function renderCoursesGrid(courses) {
  const container = document.getElementById('courses-container');
  if (!courses || courses.length === 0) {
    container.innerHTML = `<div class="text-center py-4 text-secondary">No courses found matching criteria.</div>`;
    return;
  }

  container.innerHTML = courses.map(course => {
    const max = course.MaxCapacity || course.maxCapacity || 30;
    const enrolled = course.EnrollmentCount || course.enrollmentCount || 0;
    const pct = Math.min(100, Math.round((enrolled / max) * 100));
    const isFull = enrolled >= max;

    return `
      <div class="course-card">
        <div class="course-card-header">
          <span class="course-code-badge">${escapeHtml(course.Code || course.code)}</span>
          <span class="badge ${isFull ? 'badge-danger' : 'badge-success'}">
            ${isFull ? 'Full Capacity' : 'Seats Open'}
          </span>
        </div>
        <h3 class="course-title">${escapeHtml(course.Title || course.title)}</h3>
        
        <div class="capacity-tracker">
          <div class="capacity-labels">
            <span>Enrolled: <strong>${enrolled}</strong></span>
            <span>Capacity: <strong>${max}</strong></span>
          </div>
          <div class="capacity-progress-track">
            <div class="capacity-progress-fill ${isFull ? 'full' : ''}" style="width: ${pct}%;"></div>
          </div>
        </div>

        <div class="course-card-actions">
          <button class="btn btn-secondary btn-sm btn-block" onclick="selectCourseForEnrollment('${escapeHtml(course.Code || course.code)}')">
            <i class="fa-solid fa-user-plus"></i> Quick Enroll
          </button>
        </div>
      </div>
    `;
  }).join('');
}

function filterCourses() {
  const query = document.getElementById('course-search-input').value.toLowerCase();
  const filtered = state.courses.filter(c => 
    (c.Title || c.title || '').toLowerCase().includes(query) ||
    (c.Code || c.code || '').toLowerCase().includes(query)
  );
  renderCoursesGrid(filtered);
}

/* ----------------------------------------------------
   6. Students Directory Module
   ---------------------------------------------------- */
async function loadStudents() {
  const tbody = document.getElementById('students-table-body');
  const res = await apiFetch('/api/students/all');

  if (!res.ok) {
    tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-rose">Failed to load students.</td></tr>`;
    return;
  }

  const students = Array.isArray(res.data) ? res.data : (res.data?.Items || []);
  state.students = students;

  document.getElementById('count-students').textContent = students.length;

  let totalEnrollments = 0;
  students.forEach(s => totalEnrollments += (s.EnrollmentCount || s.enrollmentCount || 0));
  document.getElementById('count-enrollments').textContent = totalEnrollments;

  renderStudentsTable(students);
  updateStudentSelects(students);
}

function renderStudentsTable(students) {
  const tbody = document.getElementById('students-table-body');
  if (!students || students.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-muted">No registered students found.</td></tr>`;
    return;
  }

  tbody.innerHTML = students.map(s => {
    const gpa = parseFloat(s.GPA || s.gpa || 0).toFixed(2);
    const gpaBadgeClass = gpa >= 3.8 ? 'badge-success' : gpa >= 3.0 ? 'badge-neutral' : 'badge-warning';

    return `
      <tr>
        <td><code>${escapeHtml(s.RegistrationNumber || s.registrationNumber)}</code></td>
        <td><strong>${escapeHtml(s.Name || s.name)}</strong></td>
        <td>${s.Age || s.age || '--'}</td>
        <td><span class="badge ${gpaBadgeClass}">${gpa} GPA</span></td>
        <td><span class="badge badge-neutral">${s.EnrollmentCount || s.enrollmentCount || 0} courses</span></td>
        <td><span class="badge ${s.IsActive || s.isActive ? 'badge-success' : 'badge-danger'}">${s.IsActive || s.isActive ? 'Active' : 'Inactive'}</span></td>
        <td>
          <button class="btn btn-secondary btn-sm" onclick="triggerTranscriptRequest(${s.Id || s.id})">
            <i class="fa-solid fa-file-waveform"></i> Transcript
          </button>
        </td>
      </tr>
    `;
  }).join('');
}

function filterStudents() {
  const query = document.getElementById('student-search-input').value.toLowerCase();
  const filtered = state.students.filter(s =>
    (s.Name || s.name || '').toLowerCase().includes(query) ||
    (s.RegistrationNumber || s.registrationNumber || '').toLowerCase().includes(query)
  );
  renderStudentsTable(filtered);
}

/* ----------------------------------------------------
   7. Enrollment Hub & Form
   ---------------------------------------------------- */
function updateCourseSelects(courses) {
  const enrollSelect = document.getElementById('enroll-course-select');
  const certSelect = document.getElementById('cert-course-select');
  const gradeSelect = document.getElementById('grade-course-select');

  const optionsHtml = '<option value="">-- Choose Course --</option>' + courses.map(c => {
    const code = c.Code || c.code;
    const title = c.Title || c.title;
    const id = c.Id || c.id;
    return `<option value="${code}" data-id="${id}">${code} - ${title}</option>`;
  }).join('');

  if (enrollSelect) enrollSelect.innerHTML = optionsHtml;
  if (certSelect) certSelect.innerHTML = optionsHtml;
  if (gradeSelect) gradeSelect.innerHTML = optionsHtml;
}

function updateStudentSelects(students) {
  const enrollSelect = document.getElementById('enroll-student-select');
  const transcriptSelect = document.getElementById('transcript-student-select');
  const certSelect = document.getElementById('cert-student-select');
  const gradeSelect = document.getElementById('grade-student-select');

  const optionsHtml = '<option value="">-- Choose Student --</option>' + students.map(s => {
    const id = s.Id || s.id;
    const name = s.Name || s.name;
    const reg = s.RegistrationNumber || s.registrationNumber;
    return `<option value="${id}">${name} (${reg})</option>`;
  }).join('');

  if (enrollSelect) enrollSelect.innerHTML = optionsHtml;
  if (transcriptSelect) transcriptSelect.innerHTML = optionsHtml;
  if (certSelect) certSelect.innerHTML = optionsHtml;
  if (gradeSelect) gradeSelect.innerHTML = optionsHtml;
}

function selectCourseForEnrollment(courseCode) {
  switchTab('tab-enrollments');
  const select = document.getElementById('enroll-course-select');
  if (select) select.value = courseCode;
}

async function handleEnrollmentSubmit(e) {
  e.preventDefault();
  const studentId = parseInt(document.getElementById('enroll-student-select').value, 10);
  const courseCode = document.getElementById('enroll-course-select').value;
  const banner = document.getElementById('enrollment-result-banner');

  if (!studentId || !courseCode) {
    showToast('Please select both a student and course.', 'error');
    return;
  }

  const res = await apiFetch('/api/v2/enrollments', {
    method: 'POST',
    body: JSON.stringify({ studentId, courseCode })
  });

  banner.classList.remove('hidden');

  if (res.ok) {
    banner.className = 'alert-banner alert-success mt-3';
    banner.innerHTML = `<i class="fa-solid fa-check-circle"></i> Successfully enrolled student in <strong>${courseCode}</strong>!`;
    showToast('Student enrolled successfully!', 'success');
    addActivityFeedItem('New Enrollment', `Student #${studentId} enrolled in ${courseCode}`, 'fa-user-check');
    loadCourses();
    loadStudents();
  } else {
    banner.className = 'alert-banner alert-danger mt-3';
    const detail = res.data?.Detail || res.data?.detail || 'Enrollment rejected by server.';
    banner.innerHTML = `<i class="fa-solid fa-triangle-exclamation"></i> ${escapeHtml(detail)}`;
    showToast(detail, 'error');
  }
}

/* ----------------------------------------------------
   8. Performance & Grades Module
   ---------------------------------------------------- */
async function loadGrades() {
  const tbody = document.getElementById('grades-table-body');
  const res = await apiFetch('/api/assessments/results');

  if (!res.ok) {
    if (res.status === 401) {
      tbody.innerHTML = `
        <tr>
          <td colspan="6" class="text-center py-4">
            <span class="badge badge-warning"><i class="fa-solid fa-lock"></i> Protected Endpoint (401 Unauthorized)</span>
            <p class="mt-3 text-secondary">Please sign in as Admin or Instructor to inspect grade results.</p>
          </td>
        </tr>
      `;
    } else {
      tbody.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-rose">Failed to load grade results.</td></tr>`;
    }
    return;
  }

  const records = Array.isArray(res.data) ? res.data : [];
  if (records.length === 0) {
    tbody.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-muted">No assessment records found.</td></tr>`;
    return;
  }

  tbody.innerHTML = records.map(r => {
    const letter = r.LetterGrade || r.letterGrade || 'Pending';
    const badgeClass = letter === 'Distinction' ? 'badge-success' : letter === 'Pass' ? 'badge-neutral' : 'badge-danger';
    const score = r.RawGrade != null ? `${parseFloat(r.RawGrade).toFixed(1)}%` : 'Not Graded';

    return `
      <tr>
        <td><strong>${escapeHtml(r.StudentName || r.studentName)}</strong></td>
        <td><code>${escapeHtml(r.CourseCode || r.courseCode)}</code> - ${escapeHtml(r.CourseTitle || r.courseTitle)}</td>
        <td><strong>${score}</strong></td>
        <td><span class="badge ${badgeClass}">${letter}</span></td>
        <td><span class="badge ${r.IsPassing || r.isPassing ? 'badge-success' : 'badge-neutral'}">${r.IsPassing || r.isPassing ? 'Passing' : 'Needs Review'}</span></td>
        <td>${r.EnrolledAt ? new Date(r.EnrolledAt).toLocaleDateString() : 'Active'}</td>
      </tr>
    `;
  }).join('');
}

/* ----------------------------------------------------
   9. Async Transcripts & Certificates Module
   ---------------------------------------------------- */
function triggerTranscriptRequest(studentId) {
  switchTab('tab-transcripts');
  const select = document.getElementById('transcript-student-select');
  if (select) select.value = studentId;
  queueTranscript();
}

async function queueTranscript() {
  const studentId = parseInt(document.getElementById('transcript-student-select').value, 10);
  const idempotencyKey = document.getElementById('idempotency-key-input').value.trim() || undefined;

  if (!studentId) {
    showToast('Please select a student.', 'error');
    return;
  }

  const progressBox = document.getElementById('transcript-job-progress');
  const fill = document.getElementById('job-progress-fill');
  const statusTitle = document.getElementById('job-status-title');
  const reportIdEl = document.getElementById('job-report-id');
  const downloadLink = document.getElementById('job-download-link');
  const badge = document.getElementById('job-percent-badge');

  progressBox.classList.remove('hidden');
  downloadLink.classList.add('hidden');
  fill.style.width = '20%';
  statusTitle.textContent = 'Enqueuing Background Channel Task...';
  badge.textContent = 'Queued';
  badge.className = 'badge badge-warning';

  const res = await apiFetch('/api/v2/transcripts', {
    method: 'POST',
    headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {},
    body: JSON.stringify({ studentId })
  });

  if (!res.ok) {
    statusTitle.textContent = 'Task Enqueue Failed';
    badge.textContent = 'Failed';
    badge.className = 'badge badge-danger';
    showToast('Failed to queue transcript job.', 'error');
    return;
  }

  const reportId = res.data?.ReportId || res.data?.reportId || 'auto-gen';
  reportIdEl.textContent = `Report ID: ${reportId}`;
  statusTitle.textContent = 'Background Worker Processing...';
  fill.style.width = '60%';
  badge.textContent = 'Processing';

  addActivityFeedItem('Transcript Queued', `Job #${reportId} dispatched to background queue`, 'fa-clock');
  showToast(`Transcript #${reportId} accepted!`, 'info');

  // Poll status
  let pollCount = 0;
  clearInterval(state.pollTimer);
  state.pollTimer = setInterval(async () => {
    pollCount++;
    const statusRes = await apiFetch(`/api/v2/transcripts/${reportId}/status`);
    if (statusRes.ok) {
      const statusData = statusRes.data;
      const currentStatus = statusData?.Status || statusData?.status;

      if (currentStatus === 'Ready' || currentStatus === 'ready') {
        clearInterval(state.pollTimer);
        fill.style.width = '100%';
        statusTitle.textContent = 'Transcript Ready for Download!';
        badge.textContent = 'Completed (100%)';
        badge.className = 'badge badge-success';
        downloadLink.href = `/api/v2/transcripts/${reportId}/download`;
        downloadLink.classList.remove('hidden');
      }
    }

    if (pollCount > 12) {
      clearInterval(state.pollTimer);
    }
  }, 2000);
}

async function issueCertificate() {
  const studentId = parseInt(document.getElementById('cert-student-select').value, 10);
  const courseCode = document.getElementById('cert-course-select').value;
  const resultBox = document.getElementById('cert-result-card');
  const heading = document.getElementById('cert-serial-heading');
  const detail = document.getElementById('cert-detail-text');

  if (!studentId || !courseCode) {
    showToast('Please choose both student and course.', 'error');
    return;
  }

  showToast('Connecting to certificate authority with Polly resilience...', 'info');

  const res = await apiFetch('/api/v2/certificates', {
    method: 'POST',
    body: JSON.stringify({ studentId, courseCode })
  });

  resultBox.classList.remove('hidden');

  if (res.ok) {
    const serial = res.data?.SerialNumber || res.data?.serialNumber || 'CERT-SUCCESS';
    heading.textContent = serial;
    detail.textContent = `Official verified certificate for ${courseCode} recorded to student ledger.`;
    showToast('Certificate successfully issued!', 'success');
    addActivityFeedItem('Certificate Issued', `Serial ${serial} granted for ${courseCode}`, 'fa-award');
    loadCertificates();
  } else {
    heading.textContent = 'Issuance Failed';
    detail.textContent = res.data?.Detail || res.data?.detail || 'Upstream service unavailable.';
    showToast('Failed to issue certificate.', 'error');
  }
}

async function loadCertificates() {
  const tbody = document.getElementById('certs-table-body');
  const res = await apiFetch('/api/v2/certificates');

  if (!res.ok) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-muted">No certificates registered yet.</td></tr>`;
    return;
  }

  const list = Array.isArray(res.data) ? res.data : [];
  if (list.length === 0) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-muted">No certificates registered yet.</td></tr>`;
    return;
  }

  tbody.innerHTML = list.map(c => `
    <tr>
      <td><code>${escapeHtml(c.SerialNumber || c.serialNumber)}</code></td>
      <td><strong>${escapeHtml(c.StudentName || c.studentName || 'Student #' + c.StudentId)}</strong></td>
      <td><code>${escapeHtml(c.CourseCode || c.courseCode || '')}</code> - ${escapeHtml(c.CourseTitle || c.courseTitle || '')}</td>
      <td>${new Date(c.IssuedAt || c.issuedAt).toLocaleString()}</td>
      <td><span class="badge badge-success"><i class="fa-solid fa-circle-check"></i> Verified</span></td>
    </tr>
  `).join('');
}

/* ----------------------------------------------------
   10. Interactive API Playground & Telemetry
   ---------------------------------------------------- */
async function runApiTester() {
  const method = document.getElementById('tester-method').value;
  const url = document.getElementById('tester-url').value.trim();
  const bodyText = document.getElementById('tester-body').value.trim();

  const viewer = document.getElementById('tester-response-viewer');
  const statusBadge = document.getElementById('response-status-badge');
  const timeBadge = document.getElementById('response-time-badge');
  const correlationIdEl = document.getElementById('meta-correlation-id');
  const deprecationEl = document.getElementById('meta-deprecation');

  viewer.textContent = '// Executing request...';

  const options = { method };
  if (['POST', 'PUT', 'PATCH'].includes(method) && bodyText) {
    try {
      options.body = JSON.stringify(JSON.parse(bodyText));
    } catch {
      options.body = bodyText;
    }
  }

  const res = await apiFetch(url, options);

  statusBadge.textContent = `Status: ${res.status} ${res.statusText}`;
  statusBadge.className = `badge ${res.ok ? 'badge-success' : 'badge-danger'}`;
  timeBadge.textContent = `Time: ${res.duration}ms`;

  correlationIdEl.textContent = res.correlationId || '--';
  deprecationEl.textContent = res.deprecation;

  if (typeof res.data === 'object') {
    viewer.textContent = JSON.stringify(res.data, null, 2);
  } else {
    viewer.textContent = String(res.data);
  }
}

/* ----------------------------------------------------
   11. Authentication Module & Modals
   ---------------------------------------------------- */
function initAuth() {
  updateNavUser();
}

function updateNavUser() {
  const authBox = document.getElementById('auth-box');
  if (!authBox) return;

  if (state.token && state.user) {
    const roles = (state.user.roles || []).join(', ') || 'User';
    authBox.innerHTML = `
      <div class="user-pill" style="display: flex; align-items: center; gap: 0.6rem; background: var(--bg-card); border: 1px solid var(--border-subtle); padding: 0.35rem 0.85rem; border-radius: var(--radius-full);">
        <div style="width: 28px; height: 28px; border-radius: 50%; background: linear-gradient(135deg, var(--color-indigo), var(--color-violet)); color: #fff; display: flex; align-items: center; justify-content: center; font-size: 0.8rem; font-weight: 700;">
          ${(state.user.firstName || 'U')[0].toUpperCase()}
        </div>
        <div style="display: flex; flex-direction: column; text-align: left; line-height: 1.1;">
          <span style="font-size: 0.825rem; font-weight: 600;">${escapeHtml(state.user.firstName || state.user.email)}</span>
          <span style="font-size: 0.68rem; color: #a5b4fc;">${roles}</span>
        </div>
        <button id="logout-btn" class="btn-icon" style="width: 26px; height: 26px; border: none; margin-left: 0.3rem;" title="Sign Out">
          <i class="fa-solid fa-right-from-bracket" style="font-size: 0.75rem;"></i>
        </button>
      </div>
    `;
    document.getElementById('logout-btn')?.addEventListener('click', logout);
  } else {
    authBox.innerHTML = `
      <button class="btn btn-primary" id="login-modal-open-btn">
        <i class="fa-solid fa-user-lock"></i>
        <span>Sign In</span>
      </button>
    `;
    document.getElementById('login-modal-open-btn')?.addEventListener('click', () => openModal('auth-modal'));
  }
}

async function handleLogin(e) {
  e.preventDefault();
  const email = document.getElementById('login-email').value.trim();
  const password = document.getElementById('login-password').value;
  const alertEl = document.getElementById('auth-alert');

  const res = await apiFetch('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });

  if (res.ok) {
    state.token = res.data.accessToken;
    state.refreshToken = res.data.refreshToken;
    state.user = res.data.user || { email, firstName: email.split('@')[0], roles: ['User'] };

    localStorage.setItem('tms_token', state.token);
    localStorage.setItem('tms_refresh_token', state.refreshToken);
    localStorage.setItem('tms_user', JSON.stringify(state.user));

    updateNavUser();
    closeModal('auth-modal');
    showToast(`Welcome back, ${state.user.firstName}!`, 'success');
    loadGrades();
  } else {
    alertEl.classList.remove('hidden');
    alertEl.className = 'alert-banner alert-danger mt-3';
    alertEl.innerHTML = `<i class="fa-solid fa-triangle-exclamation"></i> ${escapeHtml(res.data?.Detail || res.data?.detail || 'Invalid credentials')}`;
  }
}

async function handleRegister(e) {
  e.preventDefault();
  const firstName = document.getElementById('reg-name').value.trim();
  const email = document.getElementById('reg-email').value.trim();
  const password = document.getElementById('reg-password').value;
  const role = document.getElementById('reg-role').value;
  const alertEl = document.getElementById('auth-alert');

  const res = await apiFetch('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ firstName, email, password, role })
  });

  if (res.ok) {
    alertEl.classList.remove('hidden');
    alertEl.className = 'alert-banner alert-success mt-3';
    alertEl.innerHTML = `<i class="fa-solid fa-check-circle"></i> Registered successfully! You can now log in.`;
    document.getElementById('auth-tab-login').click();
    document.getElementById('login-email').value = email;
    document.getElementById('login-password').value = password;
  } else {
    alertEl.classList.remove('hidden');
    alertEl.className = 'alert-banner alert-danger mt-3';
    alertEl.innerHTML = `<i class="fa-solid fa-triangle-exclamation"></i> ${escapeHtml(res.data?.Detail || res.data?.detail || 'Registration failed')}`;
  }
}

function logout() {
  state.token = null;
  state.refreshToken = null;
  state.user = null;
  localStorage.removeItem('tms_token');
  localStorage.removeItem('tms_refresh_token');
  localStorage.removeItem('tms_user');
  updateNavUser();
  showToast('Signed out successfully.', 'info');
  loadGrades();
}

/* ----------------------------------------------------
   12. Event Listeners & Modals
   ---------------------------------------------------- */
function initEventListeners() {
  document.getElementById('theme-toggle-btn')?.addEventListener('click', toggleTheme);

  // Search inputs
  document.getElementById('course-search-input')?.addEventListener('input', filterCourses);
  document.getElementById('student-search-input')?.addEventListener('input', filterStudents);

  // Refresh buttons
  document.getElementById('refresh-courses-btn')?.addEventListener('click', loadCourses);
  document.getElementById('refresh-students-btn')?.addEventListener('click', loadStudents);
  document.getElementById('refresh-grades-btn')?.addEventListener('click', loadGrades);
  document.getElementById('refresh-certs-btn')?.addEventListener('click', loadCertificates);

  // Forms
  document.getElementById('enrollment-form')?.addEventListener('submit', handleEnrollmentSubmit);
  document.getElementById('generate-transcript-btn')?.addEventListener('click', queueTranscript);
  document.getElementById('issue-cert-btn')?.addEventListener('click', issueCertificate);

  // Modals Open
  document.getElementById('open-add-course-modal-btn')?.addEventListener('click', () => openModal('add-course-modal'));
  document.getElementById('open-add-student-modal-btn')?.addEventListener('click', () => openModal('add-student-modal'));
  document.getElementById('open-record-grade-modal-btn')?.addEventListener('click', () => openModal('record-grade-modal'));

  // Modals Close
  document.getElementById('auth-modal-close-btn')?.addEventListener('click', () => closeModal('auth-modal'));
  document.getElementById('add-course-modal-close-btn')?.addEventListener('click', () => closeModal('add-course-modal'));
  document.getElementById('add-student-modal-close-btn')?.addEventListener('click', () => closeModal('add-student-modal'));
  document.getElementById('record-grade-modal-close-btn')?.addEventListener('click', () => closeModal('record-grade-modal'));

  // Auth Tabs
  document.getElementById('auth-tab-login')?.addEventListener('click', () => {
    document.getElementById('auth-tab-login').classList.add('active');
    document.getElementById('auth-tab-register').classList.remove('active');
    document.getElementById('login-form').classList.remove('hidden');
    document.getElementById('register-form').classList.add('hidden');
  });

  document.getElementById('auth-tab-register')?.addEventListener('click', () => {
    document.getElementById('auth-tab-register').classList.add('active');
    document.getElementById('auth-tab-login').classList.remove('active');
    document.getElementById('register-form').classList.remove('hidden');
    document.getElementById('login-form').classList.add('hidden');
  });

  // Demo Credentials quick fill buttons
  document.querySelectorAll('.quick-creds-buttons .btn-chip').forEach(btn => {
    btn.addEventListener('click', () => {
      document.getElementById('login-email').value = btn.getAttribute('data-email');
      document.getElementById('login-password').value = btn.getAttribute('data-pass');
    });
  });

  // Auth Submits
  document.getElementById('login-form')?.addEventListener('submit', handleLogin);
  document.getElementById('register-form')?.addEventListener('submit', handleRegister);

  // Add Course Submit
  document.getElementById('add-course-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const code = document.getElementById('new-course-code').value.trim();
    const title = document.getElementById('new-course-title').value.trim();
    const maxCapacity = parseInt(document.getElementById('new-course-capacity').value, 10);

    const res = await apiFetch('/api/courses', {
      method: 'POST',
      body: JSON.stringify({ code, title, maxCapacity })
    });

    if (res.ok) {
      closeModal('add-course-modal');
      showToast(`Course ${code} created successfully!`, 'success');
      loadCourses();
    } else {
      showToast(res.data?.Detail || res.data?.detail || 'Failed to create course', 'error');
    }
  });

  // Add Student Submit
  document.getElementById('add-student-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const registrationNumber = document.getElementById('new-student-reg').value.trim();
    const name = document.getElementById('new-student-name').value.trim();
    const age = parseInt(document.getElementById('new-student-age').value, 10);

    const res = await apiFetch('/api/students', {
      method: 'POST',
      body: JSON.stringify({ registrationNumber, name, age, gpa: 0.0 })
    });

    if (res.ok) {
      closeModal('add-student-modal');
      showToast(`Student ${name} registered successfully!`, 'success');
      loadStudents();
    } else {
      showToast(res.data?.Detail || res.data?.detail || 'Failed to register student', 'error');
    }
  });

  // Record Grade Submit
  document.getElementById('record-grade-form')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const studentId = parseInt(document.getElementById('grade-student-select').value, 10);
    const courseSelect = document.getElementById('grade-course-select');
    const selectedOption = courseSelect.options[courseSelect.selectedIndex];
    const courseId = parseInt(selectedOption.getAttribute('data-id') || '1', 10);
    const score = parseFloat(document.getElementById('grade-score-input').value);

    const res = await apiFetch('/api/assessments/submit-grade', {
      method: 'POST',
      body: JSON.stringify({ studentId, courseId, score })
    });

    if (res.ok) {
      closeModal('record-grade-modal');
      showToast(`Grade recorded: ${res.data.LetterGrade}!`, 'success');
      loadGrades();
      loadStudents();
    } else {
      showToast(res.data?.Detail || res.data?.detail || 'Failed to record grade', 'error');
    }
  });

  // API Tester Controls
  document.getElementById('tester-send-btn')?.addEventListener('click', runApiTester);
  document.getElementById('tester-method')?.addEventListener('change', (e) => {
    const val = e.target.value;
    const bodyGroup = document.getElementById('tester-body-group');
    if (['POST', 'PUT', 'PATCH'].includes(val)) {
      bodyGroup.style.display = 'block';
    } else {
      bodyGroup.style.display = 'none';
    }
  });

  document.querySelectorAll('.preset-buttons .btn-chip').forEach(btn => {
    btn.addEventListener('click', () => {
      document.getElementById('tester-method').value = btn.getAttribute('data-method');
      document.getElementById('tester-url').value = btn.getAttribute('data-url');
      document.getElementById('tester-method').dispatchEvent(new Event('change'));
      runApiTester();
    });
  });
}

function openModal(id) {
  const modal = document.getElementById(id);
  if (modal) modal.classList.remove('hidden');
}

function closeModal(id) {
  const modal = document.getElementById(id);
  if (modal) modal.classList.add('hidden');
}

function showToast(message, type = 'info') {
  const container = document.getElementById('toast-container');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast toast-${type}`;
  const icon = type === 'success' ? 'fa-check-circle text-emerald' : type === 'error' ? 'fa-triangle-exclamation text-rose' : 'fa-info-circle text-indigo';
  
  toast.innerHTML = `<i class="fa-solid ${icon}"></i> <span>${escapeHtml(message)}</span>`;
  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateY(10px)';
    toast.style.transition = 'all 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 4000);
}

function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
